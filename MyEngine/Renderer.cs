﻿using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Runtime.OpenGL;
using MyEngine.Utils;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using System.Drawing;
using System.Numerics;
using RendererLoadError = MyEngine.Utils.OneOf<
    MyEngine.Runtime.OpenGL.FragmentShaderCompilationFailed,
    MyEngine.Runtime.OpenGL.VertexShaderCompilationFailed,
    MyEngine.Runtime.OpenGL.ShaderProgramLinkFailed,
    MyEngine.Runtime.OpenGL.VertexArrayObject.AttachElementBufferError
    >;

namespace MyEngine.Runtime;

internal sealed class Renderer : IDisposable, IResource
{
    private GL _gl = null!;
    private BufferObject<float> _vertexBuffer = null!;
    private BufferObject<uint> _elementBuffer = null!;
    private VertexArrayObject _vertexArrayObject = null!;
    private TextureObject _texture = null!;
    private ShaderProgram _shader = null!;
    private ShaderProgram _lineShader = null!;

    private BufferObject<float> _lineVertexBuffer = null!;
    private VertexArrayObject _lineVertexArray = null!;

    private static readonly uint[] Indices =
    {
        0, 1, 3,
        1, 2, 3
    };

    private static readonly float[] Vertices =
    {
        //X    Y      Z     aTextCoords
        0.5f,  0.5f, 0.0f, 1.0f, 1.0f,
        0.5f, -0.5f, 0.0f, 1.0f, 0.0f,
       -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
       -0.5f,  0.5f, 0.0f, 0.0f, 1.0f
    };

    private readonly string _vertexCode;
    private readonly string _fragmentCode;
    private readonly string _lineVertexCode;
    private readonly string _lineFragmentCode;

    private uint _width;
    private uint _height;

    private Renderer(
        string vertexCode,
        string fragmentCode,
        string lineVertexCode,
        string lineFragmentCode,
        uint width,
        uint height)
    {
        _vertexCode = vertexCode;
        _fragmentCode = fragmentCode;
        _lineVertexCode = lineVertexCode;
        _lineFragmentCode = lineFragmentCode;

        _width = width;
        _height = height;
    }

    public unsafe Result<Unit, RendererLoadError> Load(IWindow window)
    {
        // todo: try catch around all opengl stuff
        _gl = window.CreateOpenGL();

        _gl.ClearColor(Color.CornflowerBlue);

        _vertexBuffer = BufferObject<float>.CreateAndBind(_gl, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        _vertexBuffer.SetData(Vertices);

        _elementBuffer = BufferObject<uint>.CreateAndBind(_gl, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);
        _elementBuffer.SetData(Indices);

        _vertexArrayObject = VertexArrayObject.CreateAndBind(_gl, _vertexBuffer);
        if (_vertexArrayObject.AttachElementBuffer(_elementBuffer).TryGetError(out var attachElementBufferError))
        {
            return Result.Failure<Unit, RendererLoadError>(new RendererLoadError(attachElementBufferError));
        }

        _vertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, false, 5, 0); // location
        _vertexArrayObject.VertexArrayAttribute(1, 2, VertexAttribPointerType.Float, false, 5, 3); // texture coordinate

        var shaderResult = ShaderProgram.Create(_gl, _vertexCode, _fragmentCode)
            .MapError(err =>
            {
                return err.Match(
                    vertexShaderCompilationError => new RendererLoadError(vertexShaderCompilationError),
                    fragmentShaderCompilationError => new RendererLoadError(fragmentShaderCompilationError),
                    shaderLinkError => new RendererLoadError(shaderLinkError));
            });
        if (!shaderResult.TryGetValue(out var shader))
        {
            return Result.Failure<Unit, RendererLoadError>(shaderResult.UnwrapError());
        }

        _shader = shader;

        // unbind everything
        _vertexArrayObject.Unbind();
        _vertexBuffer.Unbind();
        _elementBuffer.Unbind();

        var textureBytes = File.ReadAllBytes("silk.png");
        var imageResult = ImageResult.FromMemory(textureBytes, ColorComponents.RedGreenBlueAlpha);

        _texture = new TextureObject(_gl, imageResult.Data, (uint)imageResult.Width, (uint)imageResult.Height, TextureTarget.Texture2D, TextureUnit.Texture0);

        // unbind texture
        _texture.Unbind();

        _shader.SetUniform1("uTexture", 0);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _lineVertexBuffer = BufferObject<float>.CreateAndBind(_gl, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        
        _lineVertexArray = VertexArrayObject.CreateAndBind(_gl, _lineVertexBuffer);
        _lineVertexArray.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, false, 3, 0); // location

        var lineShaderResult = ShaderProgram.Create(_gl, _lineVertexCode, _lineFragmentCode)
            .MapError(err =>
            {
                return err.Match(
                    vertexShaderCompilationError => new RendererLoadError(vertexShaderCompilationError),
                    fragmentShaderCompilationError => new RendererLoadError(fragmentShaderCompilationError),
                    shaderLinkError => new RendererLoadError(shaderLinkError));
            });
        if (!lineShaderResult.TryGetValue(out var lineShader))
        {
            return Result.Failure<Unit, RendererLoadError>(lineShaderResult.UnwrapError());
        }

        _lineShader = lineShader;

        _lineVertexArray.Unbind();
        _lineVertexBuffer.Unbind();

        return Result.Success<Unit, RendererLoadError>(Unit.Value);
    }

    public void Resize(Vector2 size)
    {
        _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        _width = (uint)size.X;
        _height = (uint)size.Y;
    }

    public readonly record struct Line(Vector3 Start, Vector3 End);

    public unsafe void RenderOrthographic(Vector3 cameraPosition, Vector2 viewSize, IEnumerable<GlobalTransform> transforms, IReadOnlyCollection<Line> lines)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _vertexArrayObject.Bind();

        _shader.UseProgram();

        _texture.Bind(TextureUnit.Texture0);

        var view = Matrix4x4.CreateLookAt(cameraPosition, cameraPosition - Vector3.UnitZ, Vector3.UnitY);
        var projection = Matrix4x4.CreateOrthographic(viewSize.X, viewSize.Y, 0.1f, 100f);

        _shader.SetUniform1("uView", view);
        _shader.SetUniform1("uProjection", projection);

        foreach (var transform in transforms)
        {
            var model = transform.ModelMatrix;

            _shader.SetUniform1("uModel", model);

            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

        if (lines.Any())
        {
            _lineVertexBuffer.Bind();
            _lineVertexBuffer.SetData(lines.SelectMany(x => new[] { x.Start.X, x.Start.Y, x.Start.Z, x.End.X, x.End.Y, x.End.Z }).ToArray());

            _lineVertexArray.Bind();

            _lineShader.UseProgram();
            _lineShader.SetUniform1("uView", view);
            _lineShader.SetUniform1("uProjection", projection);

            _gl.DrawArrays(GLEnum.Lines, 0, (uint)(lines.Count * 2));
        }
    } 

    public readonly record struct RenderError(GlobalTransform.GetPositionRotationScaleError Error);

    public unsafe Result<Unit, RenderError> Render(GlobalTransform cameraTransform, IEnumerable<GlobalTransform> transforms)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _vertexArrayObject.Bind();

        _shader.UseProgram();

        _texture.Bind(TextureUnit.Texture0);

        var positionRotationScaleResult = cameraTransform.GetPositionRotationScale()
            .MapError(err => new RenderError(err));

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, RenderError>(positionRotationScaleResult.UnwrapError());
        }

        var (cameraPosition, cameraRotation, _) = positionRotationScale;

        var cameraDirection = cameraRotation.ToEulerAngles();

        var cameraFront = Vector3.Normalize(cameraDirection);

        var view = Matrix4x4.CreateLookAt(cameraPosition, cameraPosition + cameraFront, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), _width / _height, 0.1f, 100.0f);

        _shader.SetUniform1("uView", view);
        _shader.SetUniform1("uProjection", projection);

        foreach (var transform in transforms)
        {
            var model = transform.ModelMatrix;

            _shader.SetUniform1("uModel", model);

            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

        return Result.Success<Unit, RenderError>(Unit.Value);
    }

    public static async Task<Renderer> CreateAsync(int width, int height)
    {
        var vertexTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.vert"));
        var lineVertexTask = File.ReadAllTextAsync(Path.Join("Shaders", "lineShader.vert"));
        var fragmentTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.frag"));
        var lineFragmentTask = File.ReadAllTextAsync(Path.Join("Shaders", "lineShader.frag"));

        await Task.WhenAll(vertexTask, fragmentTask, lineVertexTask, lineFragmentTask);

        return new Renderer(vertexTask.Result, fragmentTask.Result, lineVertexTask.Result, lineFragmentTask.Result, (uint)width, (uint)height);
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _elementBuffer?.Dispose();
        _vertexArrayObject?.Dispose();
        _texture?.Dispose();
    }
}
