using MyEngine.OpenGL;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using System.Drawing;

namespace MyEngine;

internal sealed class Renderer : IDisposable
{
    private readonly IWindow _window;
    private GL _gl = null!;
    private IInputContext _inputContext = null!;

    private uint _shaderProgram;
    private uint _texture;

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

    // todo: handle stuff only being set on the OnLoad callback
    private BufferObject<float> _vertexBuffer = null!;
    private BufferObject<uint> _elementBuffer = null!;

    private VertexArrayObject _vertexArrayObject = null!;

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

        // create vertexArrayObject
        _vertexArrayObject = new VertexArrayObject(_gl);

        _vertexBuffer = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        _elementBuffer = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);

        var vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, _vertexCode);
        _gl.CompileShader(vertexShader);

        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
        {
            throw new Exception($"Vertex shader failed to compile: {_gl.GetShaderInfoLog(vertexShader)}");
        }

        var fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, _fragmentCode);

        _gl.CompileShader(fragmentShader);
        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
        if (fStatus != (int)GLEnum.True)
        {
            throw new Exception($"Fragment shader failed to compile: {_gl.GetShaderInfoLog(fragmentShader)}");
        }

        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vertexShader);
        _gl.AttachShader(_shaderProgram, fragmentShader);

        _gl.LinkProgram(_shaderProgram);

        _gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
        {
            throw new Exception($"Shader program failed to link: {_gl.GetProgramInfoLog(_shaderProgram)}");
        }

        _gl.DetachShader(_shaderProgram, vertexShader);
        _gl.DetachShader(_shaderProgram, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        const uint stride = (3 * sizeof(float)) + (2 * sizeof(float));

        const uint positionLoc = 0;
        _gl.EnableVertexAttribArray(positionLoc);
        _gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, normalized: false, stride, null);

        const uint textureLoc = 1;
        const uint textureOffset = 3 * sizeof(float);
        _gl.EnableVertexAttribArray(textureLoc);
        _gl.VertexAttribPointer(textureLoc, 2, VertexAttribPointerType.Float, normalized: false, stride, (void*)textureOffset);

        // unbind everything
        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        _texture = _gl.GenTexture();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);

        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes("silk.png"), ColorComponents.RedGreenBlueAlpha);

        fixed (byte* ptr = result.Data)
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba,
                (uint)result.Width,
                (uint)result.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                ptr);
        }

        _gl.TextureParameter(_texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TextureParameter(_texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        _gl.TextureParameter(_texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TextureParameter(_texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        _gl.GenerateMipmap(TextureTarget.Texture2D);

        // unbind texture
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        int location = _gl.GetUniformLocation(_shaderProgram, "uTexture");
        _gl.Uniform1(location, 0);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    private void OnResize(Vector2D<int> size)
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

        _gl.UseProgram(_shaderProgram);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);

        _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
    }

    public void Run()
    {
        _window.Run();
    }

    public static async Task<Renderer> CreateAsync(string windowTitle, Vector2D<int> size)
    {
        var vertexTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.vert"));
        var fragmentTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.frag"));

        await Task.WhenAll(vertexTask, fragmentTask);

        var window = Window.Create(WindowOptions.Default with
        {
            Title = windowTitle,
            Size = size
        });

        return new Renderer(window, vertexTask.Result, fragmentTask.Result);
    }

    private void OnWindowClose()
    {
        _vertexBuffer.Dispose();
        _elementBuffer.Dispose();
        _vertexArrayObject.Dispose();
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
