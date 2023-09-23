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
