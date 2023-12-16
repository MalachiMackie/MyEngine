using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Assets;

internal class AssetLoadingSystem : ISystem
{
    private readonly IAssetCommands _assetCommands;
    private readonly IAssetCollection _assetCollection;

    internal AssetLoadingSystem(IAssetCommands assetCommands, IAssetCollection assetCollection)
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
                            _assetCollection.AddAsset(loadedAsset);
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
