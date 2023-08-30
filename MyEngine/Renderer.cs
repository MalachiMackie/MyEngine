using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Runtime.OpenGL;
using MyEngine.Utils;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Drawing;
using System.Numerics;
using RendererLoadError = MyEngine.Utils.OneOf<
    MyEngine.Runtime.OpenGL.FragmentShaderCompilationFailed,
    MyEngine.Runtime.OpenGL.VertexShaderCompilationFailed,
    MyEngine.Runtime.OpenGL.ShaderProgramLinkFailed
    >;

namespace MyEngine.Runtime;

internal sealed class Renderer : IDisposable, IResource
{
    private GL _gl = null!;
    private BufferObject<float> _vertexBuffer = null!;
    private BufferObject<uint> _elementBuffer = null!;
    private BufferObject<Matrix4x4> _matrixModelBuffer = null!;
    private VertexArrayObject _vertexArrayObject = null!;
    private ShaderProgram _shader = null!;
    private ShaderProgram _lineShader = null!;

    private BufferObject<float> _lineVertexBuffer = null!;
    private VertexArrayObject _lineVertexArray = null!;
    private readonly Dictionary<SpriteId, TextureObject> _textures = new();

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
       -0.5f,  0.5f, 0.0f, 0.0f, 1.0f,
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

        _matrixModelBuffer = BufferObject<Matrix4x4>.CreateAndBind(_gl, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

        _vertexArrayObject = VertexArrayObject.CreateAndBind(_gl, _vertexBuffer);
        _vertexArrayObject.AttachBuffer(_elementBuffer);

        _vertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, false, 5, 0); // location
        _vertexArrayObject.VertexArrayAttribute(1, 2, VertexAttribPointerType.Float, false, 5, 3); // texture coordinate

        _vertexArrayObject.AttachBuffer(_matrixModelBuffer);

        // model matrix needs 4 attributes, because attributes can only hold 4 values each
        _vertexArrayObject.VertexArrayAttribute(2, 4, VertexAttribPointerType.Float, false, 16, 0); // model matrix
        _vertexArrayObject.VertexArrayAttribute(3, 4, VertexAttribPointerType.Float, false, 16, 4); // model matrix
        _vertexArrayObject.VertexArrayAttribute(4, 4, VertexAttribPointerType.Float, false, 16, 8); // model matrix
        _vertexArrayObject.VertexArrayAttribute(5, 4, VertexAttribPointerType.Float, false, 16, 12); // model matrix
        _gl.VertexAttribDivisor(2, 1);
        _gl.VertexAttribDivisor(3, 1);
        _gl.VertexAttribDivisor(4, 1);
        _gl.VertexAttribDivisor(5, 1);

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
    public readonly record struct SpriteRender(Sprite Sprite, GlobalTransform Transform);

    public unsafe void RenderOrthographic(Vector3 cameraPosition, Vector2 viewSize, IEnumerable<SpriteRender> sprites, IReadOnlyCollection<Line> lines)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _vertexArrayObject.Bind();

        _shader.UseProgram();

        var view = Matrix4x4.CreateLookAt(cameraPosition, cameraPosition - Vector3.UnitZ, Vector3.UnitY);
        var projection = Matrix4x4.CreateOrthographic(viewSize.X, viewSize.Y, 0.1f, 100f);

        // todo: better sprite management. Handle sprites from within the engine, rather than from user code
        void BindOrAddAndBind(Sprite sprite)
        {
            if (!_textures.TryGetValue(sprite.Id, out var textureObject))
            {
                textureObject = new TextureObject(_gl, sprite.Data, (uint)sprite.Dimensions.X, (uint)sprite.Dimensions.Y, TextureTarget.Texture2D, TextureUnit.Texture0);
                _textures[sprite.Id] = textureObject;
            }

            textureObject.Bind(TextureUnit.Texture0);
        }
        
        _shader.SetUniform1("uView", view);
        _shader.SetUniform1("uProjection", projection);
        foreach (var spriteGrouping in sprites.GroupBy(x => x.Sprite))
        {
            BindOrAddAndBind(spriteGrouping.Key);

            _matrixModelBuffer.Bind();
            var transforms = spriteGrouping.Select(x => x.Transform.ModelMatrix).ToArray();
            _matrixModelBuffer.SetData(transforms);

            _vertexArrayObject.Bind();

            _gl.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null, (uint)(transforms.Length));
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
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }
    }
}
