using MyEngine.Core.Ecs.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace MyEngine.Rendering;

public class MyWindow : IDisposable, IResource
{
    private readonly IWindow _glWindow;

    public string Title { get; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }

    private MyWindow(string appTitle,
        uint width,
        uint height,
        IWindow window,
        Action<GL, MyWindow> load)
    {
        Title = appTitle;
        _glWindow = window;
        Width = width;
        Height = height;

        Load += (myWindow) => load.Invoke(_glWindow.CreateOpenGL(), myWindow);
        _glWindow.Load += OnLoad;
        _glWindow.Update += dt => Update?.Invoke(dt);
        _glWindow.Resize += OnResize;
    }

    public static MyWindow Create(string appTitle, uint width, uint height, Action<GL, MyWindow> load)
    {
        var glWindow = Window.Create(WindowOptions.Default with
        {
            Title = appTitle,
            Size = new Vector2D<int>((int)width, (int)height)
        });

        return new MyWindow(appTitle, width, height, glWindow, load);
    }

    public void Dispose()
    {
        _glWindow?.Dispose();
    }

    public void Run()
    {
        _glWindow?.Run();
    }

    public void Close()
    {
        _glWindow?.Close();
    }

    private void OnLoad()
    {
        Load?.Invoke(this);
        _isLoaded = true;
    }

    private void OnResize(Vector2D<int> newSize)
    {
        Width = (uint)newSize.X;
        Height = (uint)newSize.Y;
        Resize?.Invoke(newSize);
    }

    private bool _isLoaded;

    internal void AddLoadAction(Action<MyWindow> onLoad)
    {
        if (_isLoaded)
        {
            onLoad(this);
        }

        Load += onLoad;
    }

    public event Action<double>? Update;
    public event Action<Vector2D<int>>? Resize;
    private event Action<MyWindow>? Load;

    // todo: better encapsulation
    internal IWindow GlWindow => _glWindow;
}
