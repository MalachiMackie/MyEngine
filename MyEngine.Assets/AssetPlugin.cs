using System.Runtime.CompilerServices;
using MyEngine.Core;

[assembly: InternalsVisibleTo("MyEngine.Runtime")]
[assembly: InternalsVisibleTo("MyEngine.Assets.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace MyEngine.Assets;

public class AssetPlugin : IPlugin
{
    public AppBuilder Register(AppBuilder builder)
    {
        return builder.AddSystem<AssetLoadingSystem>(PostUpdateSystemStage.Instance)
            .AddResource<IAssetCommands>(new AssetCommands())
            .AddResource<IAssetCollection>(new AssetCollection());
    }
}
