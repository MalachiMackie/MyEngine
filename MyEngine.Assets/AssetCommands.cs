using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Assets;

public interface IAssetCommands : IResource
{
    AssetId LoadAsset<TAsset, TLoadAssetData>(string assetPath, TLoadAssetData loadAssetData)
        where TAsset : ILoadableAsset<TLoadAssetData>;

    internal interface IAssetCommand { } 

    internal record LoadAssetCommand(AssetId assetId, Func<Task<IAsset>> loadFunc) : IAssetCommand;

    internal IEnumerable<IAssetCommand> FlushCommands();
}

internal class AssetCommands : IAssetCommands
{
    private readonly Queue<IAssetCommands.IAssetCommand> _commandQueue = new();
    public AssetId LoadAsset<TAsset, TLoadAssetData>(string assetPath, TLoadAssetData loadAssetData) where TAsset : ILoadableAsset<TLoadAssetData>
    {
        var id = AssetId.Generate();
        _commandQueue.Enqueue(new IAssetCommands.LoadAssetCommand(
            id,
            async () =>
            {
                using var fileStream = File.OpenRead(assetPath);
                return await TAsset.LoadAsync(id, fileStream, loadAssetData);
            }));

        return id;
    }

    public IEnumerable<IAssetCommands.IAssetCommand> FlushCommands()
    {
        while (_commandQueue.TryDequeue(out var command))
        {
            yield return command;
        }
    }
}
