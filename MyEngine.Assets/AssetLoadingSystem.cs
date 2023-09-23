using System.Collections.Concurrent;
using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Assets;

public class AssetLoadingSystem : ISystem
{
    private readonly IAssetCommands _assetCommands;
    private readonly AssetCollection _assetCollection;

    private readonly ConcurrentQueue<IAsset> _completedAssets = new();

    public AssetLoadingSystem(IAssetCommands assetCommands, AssetCollection assetCollection)
    {
        _assetCommands = assetCommands;
        _assetCollection = assetCollection;
    }

    public void Run(double _)
    {
        var commands = _assetCommands.FlushCommands();
        foreach (var command in commands)
        {
            switch (command)
            {
                case IAssetCommands.LoadAssetCommand loadAssetCommand:
                    {
                        loadAssetCommand.loadFunc().ContinueWith(loadedAsset =>
                        {
                            // todo: handle failure
                            _completedAssets.Enqueue(loadedAsset.Result);
                        });
                        break;
                    }
            }
        }

        while (_completedAssets.TryDequeue(out var completedAsset))
        {
            _assetCollection.AddAsset(completedAsset);
        }
    }
}
