using MyEngine.OpenGL;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using System.Drawing;
using System.Numerics;

namespace MyEngine;

internal sealed class Renderer
{
    private GL _gl = null!;
    private IInputContext _inputContext = null!;
    private BufferObject<float> _vertexBuffer = null!;
    private BufferObject<uint> _elementBuffer = null!;
    private VertexArrayObject<float, uint> _vertexArrayObject = null!;
    private TextureObject _texture = null!;
    private ShaderProgram _shader = null!;
    private IKeyboard _primaryKeyboard = null!;

    // todo: this is temporary to get input working
    private TransformComponent _cameraTransform = null!;

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

    private uint _width;
    private uint _height;

    private Renderer(string vertexCode, string fragmentCode, uint width, uint height)
    {
        _vertexCode = vertexCode;
        _fragmentCode = fragmentCode;

        _width = width;
        _height = height;
        
        // _window.FramebufferResize += OnResize;
        // _window.Render += OnRender;
        // _window.Update += OnUpdate;
        // _window.Closing += OnWindowClose;
    }

    public unsafe void OnLoad(IWindow window)
    {
        _gl = window.CreateOpenGL();
        _inputContext = window.CreateInput();
        _primaryKeyboard = _inputContext.Keyboards.First();

        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
        }

        _inputContext.Mice.First().MouseMove += OnMouseMove;

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
            // Close();
        }
    }

    public void OnUpdate(double dt)
    {
        var cameraTransform = _cameraTransform.Transform;
        var cameraDirection = MathHelper.ToEulerAngles(_cameraTransform.Transform.rotation);

        var cameraFront = Vector3.Normalize(cameraDirection);

        var speed = 5.0f * (float)dt;
        if (_primaryKeyboard.IsKeyPressed(Key.W))
        {
            cameraTransform.position += (speed * cameraFront);
        }
        if (_primaryKeyboard.IsKeyPressed(Key.S))
        {
            cameraTransform.position -= (speed * cameraFront);
        }
        if (_primaryKeyboard.IsKeyPressed(Key.A))
        {
            cameraTransform.position -= speed * Vector3.Normalize(Vector3.Cross(cameraFront, Vector3.UnitY));
        }
        if (_primaryKeyboard.IsKeyPressed(Key.D))
        {
            cameraTransform.position += speed * Vector3.Normalize(Vector3.Cross(cameraFront, Vector3.UnitY));
        }
    }

    private Vector2 _lastMousePosition;

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        var lookSensitivity = 0.1f;

        if (_lastMousePosition != default)
        {
            var yOffset = position.Y - _lastMousePosition.Y;
            var xOffset = position.X - _lastMousePosition.X;

            var q = _cameraTransform.Transform.rotation;

            var direction = MathHelper.ToEulerAngles(q);

            direction.X += xOffset * lookSensitivity;
            direction.Y -= yOffset * lookSensitivity;

            _cameraTransform.Transform.rotation = MathHelper.ToQuaternion(direction);
        }

        _lastMousePosition = position;
    }

    public unsafe void Render(double dt, TransformComponent cameraTransform)
    {
        _cameraTransform = cameraTransform;
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _vertexArrayObject.Bind();

        _shader.UseProgram();

        _texture.Bind(TextureUnit.Texture0);

        var cameraDirection = MathHelper.ToEulerAngles(cameraTransform.Transform.rotation);

        var cameraFront = Vector3.Normalize(cameraDirection);

        var view = Matrix4x4.CreateLookAt(cameraTransform.Transform.position, cameraTransform.Transform.position + cameraFront, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), _width / _height, 0.1f, 100.0f);

        _shader.SetUniform1("uView", view);
        _shader.SetUniform1("uProjection", projection);

        foreach (var transform in _transforms)
        {
            var model = transform.ViewMatrix;

            _shader.SetUniform1("uModel", model);

            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

    }

    public static async Task<Renderer> CreateAsync(string windowTitle, int width, int height)
    {
        var vertexTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.vert"));
        var fragmentTask = File.ReadAllTextAsync(Path.Join("Shaders", "shader.frag"));

        await Task.WhenAll(vertexTask, fragmentTask);

        return new Renderer(vertexTask.Result, fragmentTask.Result, (uint)width, (uint)height);
    }

    private void OnWindowClose()
    {
        _vertexBuffer?.Dispose();
        _elementBuffer?.Dispose();
        _vertexArrayObject?.Dispose();
        _texture?.Dispose();
    }
}
