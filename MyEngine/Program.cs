using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Drawing;
using System.Runtime.CompilerServices;

// https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%201.2%20-%20Hello%20quad/Program.cs 

internal class Program
{
    private static IWindow _window;
    private static GL _gl;
    private static IInputContext _inputContext;

    private static uint _vertexArrayObject;
    private static uint _vertexBufferObject;
    private static uint _elementBufferObject;

    private static uint _shaderProgram;

        const string vertexCode = @"
#version 330 core

layout (location = 0) in vec3 aPosition;

out float out_red;

void main()
{
    gl_Position = vec4(aPosition, 1.0);
}";

        const string fragmentCode = @"
#version 330 core

out vec4 out_color;

void main()
{
    out_color = vec4(1.0, 0.5, 0.2, 1.0);
}";

    private static readonly uint[] Indices =
    {
        0, 1, 3,
        1, 2, 3
    };

    private static readonly float[] Vertices =
        {
            //X    Y      Z
             0.5f,  0.5f, 0.0f,
             0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.5f
        };


    private static void Main(string[] args)
    {
        var options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "My OpenGL App"
        };

        _window = Window.Create(options);

        _window.Render += OnRender;
        _window.Load += OnLoad;
        _window.Update += OnUpdate;

        _window.Run();

        _window.Dispose();
    }

    private static void KeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        Console.WriteLine("KeyCode pressed. Key: {0}, KeyCode: {1}", key, keyCode);
        if (key == Key.Escape)
        {
            _window.Close();
        }
    }

    private unsafe static void OnLoad()
    {
        Console.WriteLine("Window Loaded");

        _inputContext = _window.CreateInput();
        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown += KeyDown;
        }

        Console.WriteLine("Initialized Input");

        // create openGL
        _gl = _window.CreateOpenGL();
        _gl.ClearColor(Color.CornflowerBlue);

        // create vertexArrayObject
        _vertexArrayObject = _gl.GenVertexArray();
        _gl.BindVertexArray(_vertexArrayObject);

        // create the vertexBufferObject
        _vertexBufferObject = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBufferObject);

        fixed (float* buf = Vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }

        // create elementBufferObject
        _elementBufferObject = _gl.GenBuffer();

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _elementBufferObject);

        fixed (uint* buf = Indices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(Indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }

        var vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexCode);
        _gl.CompileShader(vertexShader);

        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
        {
            throw new Exception($"Vertex shader failed to compile: {_gl.GetShaderInfoLog(vertexShader)}");
        }

        var fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentCode);

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

        const uint positionLoc = 0;
        _gl.EnableVertexAttribArray(positionLoc);
        _gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, normalized: false, 3 * sizeof(float), null);

        // unbind everything
        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        Console.WriteLine("Initialized OpenGL");
    }

    static void OnUpdate(double dt)
    {
    }

    private unsafe static void OnRender(double dt)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _gl.BindVertexArray(_vertexArrayObject);

        _gl.UseProgram(_shaderProgram);
        _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
    }
}