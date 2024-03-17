using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MyEngine.Runtime")]
[assembly: InternalsVisibleTo("MyEngine.Physics")]
[assembly: InternalsVisibleTo("MyEngine.Core.Tests")]
[assembly: InternalsVisibleTo("MyEngine.SourceGenerator.Tests")]
[assembly: InternalsVisibleTo("MyEngine.Rendering.Tests")]
[assembly: InternalsVisibleTo("MyEngine.UI.Tests")]

namespace MyEngine.Core;

public class CorePlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder
            .AddSystemStage(PreUpdateSystemStage.Instance, 2)
            .AddSystemStage(UpdateSystemStage.Instance, 3)
            .AddSystemStage(PostUpdateSystemStage.Instance, 4)
            .AddSystem<TransformSyncSystem>(PostUpdateSystemStage.Instance);
    }
}
