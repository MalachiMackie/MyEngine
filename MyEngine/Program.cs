using MyEngine.Runtime;

[assembly: EngineRuntimeAssembly]

internal class Program
{
    private static void Main(string[] args)
    {
        var ecsEngine = new EcsEngine();

        ecsEngine.Run();
    }
}