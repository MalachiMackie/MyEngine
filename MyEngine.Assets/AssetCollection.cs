using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Assets;

public interface IAssetCollection : IResource
{
    public Result<TAsset?> TryGetAsset<TAsset>(AssetId id);
    internal Result<Unit> AddAsset(IAsset asset);
}

public class AssetCollection : IAssetCollection
{
    private readonly Dictionary<AssetId, IAsset> _assets = new();


    public Result<Unit> AddAsset(IAsset asset)
    {
        if (!_assets.TryAdd(asset.Id, asset))
        {
            return Result.Failure<Unit>($"Asset with id {asset.Id} has already been added to the asset collection");
        }

        return Result.Success<Unit>(Unit.Value);
    }


    public Result<TAsset?> TryGetAsset<TAsset>(AssetId id)
    {
        if (!_assets.TryGetValue(id, out var asset))
        {
            return Result.Success<TAsset?>(default);
        }

        if (asset is not TAsset tAsset)
        {
            return Result.Failure<TAsset?>($"Tried adding asset {id} with the incorrect type. Specified Type: {typeof(TAsset).Name}. Actual Type: {asset.GetType().Name}");
        }

        return Result.Success<TAsset?>(tAsset);
    }
}
