using System.Runtime.CompilerServices;

namespace MyEngine.SourceGenerator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
