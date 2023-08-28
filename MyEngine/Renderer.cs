using MyEngine.Core;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Runtime.OpenGL;
using MyEngine.Utils;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using System.Drawing;
using System.Numerics;

namespace MyEngine.Runtime;

internal sealed class Renderer : IDisposable, IResource
{
    private GL _gl = null!;
    private BufferObject<float> _vertexBuffer = null!;
    private BufferObject<uint> _elementBuffer = null!;
    private VertexArrayObject<float, uint> _vertexArrayObject = null!;
    private TextureObject _texture = null!;
    private ShaderProgram _shader = null!;

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

    private uint _width;
    private uint _height;

    private Renderer(string vertexCode, string fragmentCode, uint width, uint height)
    {
        _vertexCode = vertexCode;
        _fragmentCode = fragmentCode;

        _width = width;
        _height = height;
    }

    public unsafe void Load(IWindow window)
    {
        _gl = window.CreateOpenGL();

        _gl.ClearColor(Color.CornflowerBlue);

        _vertexBuffer = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        _elementBuffer = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);

        _vertexArrayObject = new VertexArrayObject<float, uint>(_gl, _vertexBuffer, _elementBuffer);

        _vertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, false, 5, 0); // location
        _vertexArrayObject.VertexArrayAttribute(1, 2, VertexAttribPointerType.Float, false, 5, 3); // texture coordinate

        _shader = new ShaderProgram(_gl, _vertexCode, _fragmentCode);

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
    }

    public void Resize(Vector2 size)
    {
        _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        _width = (uint)size.X;
        _height = (uint)size.Y;
    }

    public unsafe void RenderOrthographic(Vector3 cameraPosition, Vector2 viewSize, IEnumerable<GlobalTransform> transforms)
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

    } 

    public unsafe void Render(GlobalTransform cameraTransform, IEnumerable<GlobalTransform> transforms)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _vertexArrayObject.Bind();

        _shader.UseProgram();

        _texture.Bind(TextureUnit.Texture0);

        var (cameraPosition, cameraRotation, _) = cameraTransform.GetPositionRotationScale();

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

    }

    public static async Task<Renderer> CreateAsync(int width, int height)
    {
        var vertexTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.vert"));
        var fragmentTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.frag"));

        await Task.WhenAll(vertexTask, fragmentTask);

        return new Renderer(vertexTask.Result, fragmentTask.Result, (uint)width, (uint)height);
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _elementBuffer?.Dispose();
        _vertexArrayObject?.Dispose();
        _texture?.Dispose();
    }
}
