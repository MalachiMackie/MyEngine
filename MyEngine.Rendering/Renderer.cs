using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Rendering.OpenGL;
using MyEngine.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Drawing;
using System.Numerics;

using RendererLoadError = MyEngine.Utils.OneOf<
    MyEngine.Rendering.OpenGL.FragmentShaderCompilationFailed,
    MyEngine.Rendering.OpenGL.VertexShaderCompilationFailed,
    MyEngine.Rendering.OpenGL.ShaderProgramLinkFailed
    >;

namespace MyEngine.Rendering;

public sealed class Renderer : IDisposable, IResource
{
    private Renderer(GL openGL,
        BufferObject<float> vertexBuffer,
        BufferObject<uint> elementBuffer,
        BufferObject<Matrix4x4> matrixModelBuffer,
        VertexArrayObject vertexArrayObject,
        ShaderProgram shader,
        ShaderProgram lineShader,
        BufferObject<float> lineVertexBuffer,
        VertexArrayObject lineVertexArray)
    {
        OpenGL = openGL;

        _vertexBuffer = vertexBuffer;
        _elementBuffer = elementBuffer;
        _matrixModelBuffer = matrixModelBuffer;
        _vertexArrayObject = vertexArrayObject;
        _shader = shader;
        _lineShader = lineShader;
        _lineVertexBuffer = lineVertexBuffer;
        _lineVertexArray = lineVertexArray;
    }

    internal GL OpenGL { get; }
    private readonly BufferObject<float> _vertexBuffer;
    private readonly BufferObject<uint> _elementBuffer;
    private readonly BufferObject<Matrix4x4> _matrixModelBuffer;
    private readonly VertexArrayObject _vertexArrayObject;
    private readonly ShaderProgram _shader;
    private readonly ShaderProgram _lineShader;

    private readonly BufferObject<float> _lineVertexBuffer;
    private readonly VertexArrayObject _lineVertexArray;
    private readonly Dictionary<AssetId, TextureObject> _textures = new();

    private static readonly uint[] Indices =
    {
        0, 1, 3,
        1, 2, 3
    };

    // todo: have these dynamic based on Sprite.PixelsPerUnit
    private static readonly float[] Vertices =
    {
        //X    Y      Z     aTextCoords
        0.5f,  0.5f, 0.0f, 1.0f, 1.0f,
        0.5f, -0.5f, 0.0f, 1.0f, 0.0f,
       -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
       -0.5f,  0.5f, 0.0f, 0.0f, 1.0f,
    };

    private uint _width;
    private uint _height;

    internal static unsafe Result<Renderer, RendererLoadError> Create(GL openGL)
    {
        // todo: try catch around all opengl stuff
        openGL.ClearColor(Color.CornflowerBlue);

        var vertexBuffer = BufferObject<float>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        vertexBuffer.SetData(Vertices);

        var elementBuffer = BufferObject<uint>.CreateAndBind(openGL, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);
        elementBuffer.SetData(Indices);

        var matrixModelBuffer = BufferObject<Matrix4x4>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

        var vertexArrayObject = VertexArrayObject.CreateAndBind(openGL, vertexBuffer);
        vertexArrayObject.AttachBuffer(elementBuffer);

        vertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, false, 5, 0); // location
        vertexArrayObject.VertexArrayAttribute(1, 2, VertexAttribPointerType.Float, false, 5, 3); // texture coordinate

        vertexArrayObject.AttachBuffer(matrixModelBuffer);

        // model matrix needs 4 attributes, because attributes can only hold 4 values each
        vertexArrayObject.VertexArrayAttribute(2, 4, VertexAttribPointerType.Float, false, 16, 0); // model matrix
        vertexArrayObject.VertexArrayAttribute(3, 4, VertexAttribPointerType.Float, false, 16, 4); // model matrix
        vertexArrayObject.VertexArrayAttribute(4, 4, VertexAttribPointerType.Float, false, 16, 8); // model matrix
        vertexArrayObject.VertexArrayAttribute(5, 4, VertexAttribPointerType.Float, false, 16, 12); // model matrix
        openGL.VertexAttribDivisor(2, 1);
        openGL.VertexAttribDivisor(3, 1);
        openGL.VertexAttribDivisor(4, 1);
        openGL.VertexAttribDivisor(5, 1);

        // todo: custom shaders
        var vertexCode = File.ReadAllText(Path.Join("Shaders", "shader.vert"));
        var lineVertexCode = File.ReadAllText(Path.Join("Shaders", "lineShader.vert"));
        var fragmentCode = File.ReadAllText(Path.Join("Shaders", "shader.frag"));
        var lineFragmentCode = File.ReadAllText(Path.Join("Shaders", "lineShader.frag"));

        var shaderResult = ShaderProgram.Create(openGL, vertexCode, fragmentCode)
            .MapError(err =>
            {
                return err.Match(
                    vertexShaderCompilationError => new RendererLoadError(vertexShaderCompilationError),
                    fragmentShaderCompilationError => new RendererLoadError(fragmentShaderCompilationError),
                    shaderLinkError => new RendererLoadError(shaderLinkError));
            });
        if (!shaderResult.TryGetValue(out var shader))
        {
            return Result.Failure<Renderer, RendererLoadError>(shaderResult.UnwrapError());
        }

        // unbind everything
        vertexArrayObject.Unbind();
        vertexBuffer.Unbind();
        elementBuffer.Unbind();

        openGL.Enable(EnableCap.Blend);
        openGL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var lineVertexBuffer = BufferObject<float>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);

        var lineVertexArray = VertexArrayObject.CreateAndBind(openGL, lineVertexBuffer);
        lineVertexArray.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, false, 3, 0); // location

        var lineShaderResult = ShaderProgram.Create(openGL, lineVertexCode, lineFragmentCode)
            .MapError(err =>
            {
                return err.Match(
                    vertexShaderCompilationError => new RendererLoadError(vertexShaderCompilationError),
                    fragmentShaderCompilationError => new RendererLoadError(fragmentShaderCompilationError),
                    shaderLinkError => new RendererLoadError(shaderLinkError));
            });
        if (!lineShaderResult.TryGetValue(out var lineShader))
        {
            return Result.Failure<Renderer, RendererLoadError>(lineShaderResult.UnwrapError());
        }

        lineVertexArray.Unbind();
        lineVertexBuffer.Unbind();

        var renderer = new Renderer(openGL,
            vertexBuffer,
            elementBuffer,
            matrixModelBuffer,
            vertexArrayObject,
            shader,
            lineShader,
            lineVertexBuffer,
            lineVertexArray);

        return Result.Success<Renderer, RendererLoadError>(renderer);
    }

    public void Resize(Vector2D<int> size)
    {
        OpenGL.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        _width = (uint)size.X;
        _height = (uint)size.Y;
    }

    public readonly record struct Line(Vector3 Start, Vector3 End);
    public readonly record struct SpriteRender(Sprite Sprite, GlobalTransform Transform);

    public unsafe void RenderOrthographic(Vector3 cameraPosition, Vector2 viewSize, IEnumerable<SpriteRender> sprites, IReadOnlyCollection<Line> lines)
    {
        OpenGL.Clear(ClearBufferMask.ColorBufferBit);

        _vertexArrayObject.Bind();

        _shader.UseProgram();

        var view = Matrix4x4.CreateLookAt(cameraPosition, cameraPosition - Vector3.UnitZ, Vector3.UnitY);
        var projection = Matrix4x4.CreateOrthographic(viewSize.X, viewSize.Y, 0.1f, 100f);

        // todo: better sprite management. Handle sprites from within the engine, rather than from user code
        void BindOrAddAndBind(Sprite sprite)
        {
            if (!_textures.TryGetValue(sprite.Id, out var textureObject))
            {
                textureObject = new TextureObject(OpenGL, sprite.Data, (uint)sprite.Dimensions.X, (uint)sprite.Dimensions.Y, TextureTarget.Texture2D, TextureUnit.Texture0);
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

            OpenGL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null, (uint)transforms.Length);
        }

        if (lines.Any())
        {
            _lineVertexBuffer.Bind();
            _lineVertexBuffer.SetData(lines.SelectMany(x => new[] { x.Start.X, x.Start.Y, x.Start.Z, x.End.X, x.End.Y, x.End.Z }).ToArray());

            _lineVertexArray.Bind();

            _lineShader.UseProgram();
            _lineShader.SetUniform1("uView", view);
            _lineShader.SetUniform1("uProjection", projection);

            OpenGL.DrawArrays(GLEnum.Lines, 0, (uint)(lines.Count * 2));
        }
    }

    public readonly record struct RenderError(GlobalTransform.GetPositionRotationScaleError Error);

    public unsafe Result<Unit, RenderError> Render(GlobalTransform cameraTransform, IEnumerable<GlobalTransform> transforms)
    {
        OpenGL.Clear(ClearBufferMask.ColorBufferBit);

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

            OpenGL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

        return Result.Success<Unit, RenderError>(Unit.Value);
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
