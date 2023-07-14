using MyEngine.OpenGL;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using System.Drawing;
using System.Numerics;

namespace MyEngine;

internal sealed class Renderer : IDisposable
{
    private readonly IWindow _window;
    private GL _gl = null!;
    private IInputContext _inputContext = null!;
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
        -0.5f,  0.5f, 0.5f, 0.0f, 1.0f
    };

    private readonly string _vertexCode;
    private readonly string _fragmentCode;

    private Transform[] _transforms = {
        new Transform {
            position = new Vector3(),
            rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 15),
            scale = Vector3.One
        },
        new Transform
        {
            position = new Vector3(-0.75f, -0.75f, 0),
            rotation = Quaternion.Identity,
            scale = new Vector3(0.25f)
        },
        new Transform
        {
            position = new Vector3(-0.5f, 0, -0.1f),
            rotation = Quaternion.Identity,
            scale = Vector3.One
        },
        new Transform
        {
            position = new Vector3(0.5f, 0, 0),
            rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 45),
            scale = Vector3.One
        }
    };

    private Renderer(IWindow window, string vertexCode, string fragmentCode)
    {
        _window = window;
        _vertexCode = vertexCode;
        _fragmentCode = fragmentCode;
        
        _window.Load += OnLoad;
        _window.FramebufferResize += OnResize;
        _window.Render += OnRender;
        _window.Closing += OnWindowClose;
    }

    private unsafe void OnLoad()
    {
        _gl = _window.CreateOpenGL();
        _inputContext = _window.CreateInput();

        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
        }

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

    private void OnResize(Silk.NET.Maths.Vector2D<int> size)
    {
        _gl.Viewport(0, 0, (uint) size.X, (uint) size.Y);
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        Console.WriteLine("KeyCode pressed. Key: {0}, KeyCode: {1}", key, keyCode);
        if (key == Key.Escape)
        {
            Close();
        }
    }

    private unsafe void OnRender(double dt)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _vertexArrayObject.Bind();

        _shader.UseProgram();

        _texture.Bind(TextureUnit.Texture0);

        foreach (var transform in _transforms)
        {
            _shader.SetUniform1("uModel", transform.ViewMatrix);
            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

    }

    public void Run()
    {
        _window.Run();
    }

    public static async Task<Renderer> CreateAsync(string windowTitle, int width, int height)
    {
        var vertexTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.vert"));
        var fragmentTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.frag"));

        await Task.WhenAll(vertexTask, fragmentTask);

        var window = Window.Create(WindowOptions.Default with
        {
            Title = windowTitle,
            Size = new Silk.NET.Maths.Vector2D<int>(width, height)
        });

        return new Renderer(window, vertexTask.Result, fragmentTask.Result);
    }

    private void OnWindowClose()
    {
        _vertexBuffer?.Dispose();
        _elementBuffer?.Dispose();
        _vertexArrayObject?.Dispose();
        _texture?.Dispose();
    }

    public void Close()
    {
        _window.Close();
    }

    public void Dispose()
    {
        _window.Dispose();
    }
}
