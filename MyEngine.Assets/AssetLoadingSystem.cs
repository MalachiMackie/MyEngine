using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Assets;

public class AssetLoadingSystem : ISystem
{
    private readonly IAssetCommands _assetCommands;
    private readonly IAssetCollection _assetCollection;

    public AssetLoadingSystem(IAssetCommands assetCommands, IAssetCollection assetCollection)
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
                        Task.Run(async () =>
                        {
                            // todo: handle failure
                            var loadedAsset = await loadAssetCommand.loadFunc();
                            if (loadedAsset is not null)
                            {
                                _assetCollection.AddAsset(loadedAsset);
                            }
                            else
                            {
                                // todo: mark as failure
                            }
                        });
                        break;
                    }
                case IAssetCommands.CreateAssetCommand createAssetCommand:
                    {
                        var result = createAssetCommand.createFunc();
                        _assetCollection.AddAsset(result);
                        break;
                    }
            }
        }
    }
}
