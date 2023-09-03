namespace MyEngine.Core;

public class CorePlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder
            .AddSystemStage(UpdateSystemStage.Instance, 2)
            .AddSystemStage(PostUpdateSystemStage.Instance, 3)
            .AddSystem<TransformSyncSystem>(PostUpdateSystemStage.Instance);
    }
}
