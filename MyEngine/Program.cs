using System.Reflection;
using MyEngine.Core;
using MyEngine.Core.Ecs;
using MyEngine.Runtime;

[assembly: EngineRuntimeAssembly]

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Must provide path to game dll");
            return;
        }

        var a = Assembly.LoadFile(args[0]);

        if (a is null)
        {
            Console.WriteLine("Could not load game dll");
            return;
        }

        var glueType = a.GetType("MyEngine.Runtime.EcsEngineGlue");

        if (glueType is null)
        {
            Console.WriteLine("Could not load game dll");
            return;
        }

        var glueInstance = Activator.CreateInstance(glueType);
        if (glueInstance is not IEcsEngineGlue ecsEngineGlue)
        {
            Console.WriteLine("Could not load game dll");
            return;
        }

        var ecsEngine = new EcsEngine(ecsEngineGlue);

        ecsEngine.Run();
    }
}