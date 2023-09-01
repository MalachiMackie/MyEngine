using MyEngine.Core;
using MyEngine.Runtime;
using MyGame;

internal class Program
{
    private static void Main(string[] args)
    {

        var appEntrypoint = new AppEntrypoint();
        var appBuilder = new AppBuilder();

        appEntrypoint.BuildApp(appBuilder);

        var ecsEngine = new EcsEngine(appBuilder);

        ecsEngine.Run();
    }
}