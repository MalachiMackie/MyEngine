using MyEngine.Core;

namespace MyEngine.Assets;

public class AssetPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder.AddSystem<AssetLoadingSystem>(PostUpdateSystemStage.Instance)
            .AddResource<IAssetCommands>(new AssetCommands())
            .AddResource(new AssetCollection());
    }
}
