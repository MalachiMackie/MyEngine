using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Assets;

public interface IAssetCommands : IResource
{
    AssetId LoadAsset<TAsset, TLoadAssetData>(string assetPath, TLoadAssetData loadAssetData)
        where TAsset : ILoadableAsset<TAsset, TLoadAssetData>;

    AssetId LoadAsset<TAsset>(string assetPath)
        where TAsset : ILoadableAsset<TAsset>;

    AssetId CreateAsset<TAsset, TCreateAssetData>(TCreateAssetData createAssetData)
        where TAsset : ICreatableAsset<TAsset, TCreateAssetData>;

    internal interface IAssetCommand { } 

    internal record LoadAssetCommand(AssetId assetId, Func<Task<IAsset>> loadFunc) : IAssetCommand;
    internal record CreateAssetCommand(AssetId assetId, Func<IAsset> createFunc) : IAssetCommand;

    internal IEnumerable<IAssetCommand> FlushCommands();
}

internal class AssetCommands : IAssetCommands
{
    private readonly Queue<IAssetCommands.IAssetCommand> _commandQueue = new();
    public AssetId LoadAsset<TAsset, TLoadAssetData>(string assetPath, TLoadAssetData loadAssetData) where TAsset : ILoadableAsset<TAsset, TLoadAssetData>
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

    public AssetId CreateAsset<TAsset, TCreateAssetData>(TCreateAssetData createAssetData) where TAsset : ICreatableAsset<TAsset, TCreateAssetData>
    {
        var id = AssetId.Generate();
        _commandQueue.Enqueue(new IAssetCommands.CreateAssetCommand(
            id,
            () =>
            {
                return TAsset.Create(id, createAssetData);
            }));

        return id;
    }

    public AssetId LoadAsset<TAsset>(string assetPath) where TAsset : ILoadableAsset<TAsset>
    {
        var id = AssetId.Generate();

        _commandQueue.Enqueue(new IAssetCommands.LoadAssetCommand(
            id,
            async () =>
            {
                using var fileStream = File.OpenRead(assetPath);
                return await TAsset.LoadAsync(id, fileStream);
            }));

        return id;
    }
}
