using MyEngine.Core.Ecs.Systems;

namespace MyEngine.Assets;

internal class AssetLoadingSystem : ISystem
{
    private readonly IAssetCommands _assetCommands;
    private readonly IEditableAssetCollection _editableAssetCollection;

    internal AssetLoadingSystem(IAssetCommands assetCommands, IEditableAssetCollection editableAssetCollection)
    {
        _assetCommands = assetCommands;
        _editableAssetCollection = editableAssetCollection;
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
                            _editableAssetCollection.AddAsset(loadedAsset);
                        });
                        break;
                    }
                case IAssetCommands.CreateAssetCommand createAssetCommand:
                    {
                        var result = createAssetCommand.createFunc();
                        _editableAssetCollection.AddAsset(result);
                        break;
                }
            }
        }
    }
}
