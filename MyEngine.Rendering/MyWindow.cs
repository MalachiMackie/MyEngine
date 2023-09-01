using MyEngine.Core.Ecs.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace MyEngine.Rendering;

public class MyWindow : IDisposable, IResource
{
    private IWindow? _window;
    private readonly string _appTitle;
    private readonly uint _width;
    private readonly uint _height;

    public MyWindow(string appTitle,
        uint width,
        uint height)
    {
        _appTitle = appTitle;
        _width = width;
        _height = height;
    }

    public void Initialize(
        Action<GL> load,
        Action<Vector2D<int>> resize)
    {
        _window = Window.Create(WindowOptions.Default with
        {
            Title = _appTitle,
            Size = new Vector2D<int>((int)_width, (int)_height)
        });

        Load += () => load?.Invoke(_window.CreateOpenGL());

        _window.Load += OnLoad;
        _window.Update += dt => Update?.Invoke(dt);
        _window.Resize += resize;
    }

    public void Dispose()
    {
        _window?.Dispose();
    }

    public void Run()
    {
        _window?.Run();
    }

    public void Close()
    {
        _window?.Close();
    }

    private void OnLoad()
    {
        Load?.Invoke();
        _isLoaded = true;
    }

    private bool _isLoaded; 

    public void AddLoadAction(Action onLoad)
    {
        if (_isLoaded)
        {
            onLoad();
        }

        Load += onLoad;
    }

    public event Action<double>? Update;
    private event Action Load;

    // todo: better encapsulation
    public IWindow? InnerWindow => _window;
}
