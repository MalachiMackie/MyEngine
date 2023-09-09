using MyEngine.Core;
using MyEngine.Runtime;
using MyGame;

internal class Program
{
    private static void Main(string[] args)
    {
        var ecsEngine = new EcsEngine();

        ecsEngine.Run();
    }
}