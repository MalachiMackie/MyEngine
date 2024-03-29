using System.IO.Abstractions;
using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Assets;

public interface IAssetCommands : IResource
{
    public AssetId LoadAsset<TAsset, TLoadAssetData>(string assetPath, TLoadAssetData loadAssetData)
        where TAsset : ILoadableAsset<TAsset, TLoadAssetData>;

    public AssetId LoadAsset<TAsset>(string assetPath)
        where TAsset : ILoadableAsset<TAsset>;

    public AssetId CreateAsset<TAsset, TCreateAssetData>(TCreateAssetData createAssetData)
        where TAsset : ICreatableAsset<TAsset, TCreateAssetData>;

    public interface IAssetCommand { }

    internal record LoadAssetCommand(AssetId assetId, Func<Task<IAsset?>> loadFunc) : IAssetCommand;
    internal record CreateAssetCommand(AssetId assetId, Func<IAsset> createFunc) : IAssetCommand;

    public IEnumerable<IAssetCommand> FlushCommands();
}

public class AssetCommands : IAssetCommands
{
    private readonly IFileSystem _fileSystem;

    public AssetCommands(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public AssetCommands()
    {
        _fileSystem = new FileSystem();
    }

    private readonly Queue<IAssetCommands.IAssetCommand> _commandQueue = new();
    public AssetId LoadAsset<TAsset, TLoadAssetData>(string assetPath, TLoadAssetData loadAssetData) where TAsset : ILoadableAsset<TAsset, TLoadAssetData>
    {
        var id = AssetId.Generate();
        _commandQueue.Enqueue(new IAssetCommands.LoadAssetCommand(
            id,
            async () =>
            {
                try
                {

                    using var fileStream = _fileSystem.File.OpenRead(assetPath);
                    return await TAsset.LoadAsync(id, fileStream, loadAssetData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to read asset: {0}", ex);
                    return null;
                }
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
                try
                {
                    using var fileStream = _fileSystem.File.OpenRead(assetPath);
                    return await TAsset.LoadAsync(id, fileStream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to load asset: {0}", ex);
                    return null;
                }
            }));

        return id;
    }
}
