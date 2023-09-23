using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Assets;

public class AssetCollection : IResource
{
    private readonly Dictionary<AssetId, IAsset> _assets = new();

    internal enum AddAssetError
    {
        AssetIdAlreadyAdded
    }

    internal Result<Unit, AddAssetError> AddAsset(IAsset asset)
    {
        if (!_assets.TryAdd(asset.Id, asset))
        {
            return Result.Failure<Unit, AddAssetError>(AddAssetError.AssetIdAlreadyAdded);
        }

        return Result.Success<Unit, AddAssetError>(Unit.Value);
    }

    public enum GetAssetError
    {
        AssetIdNotFound,
        IncorrectAssetType
    }

    public Result<TAsset, GetAssetError> TryGetAsset<TAsset>(AssetId id)
    {
        if (!_assets.TryGetValue(id, out var asset))
        {
            return Result.Failure<TAsset, GetAssetError>(GetAssetError.AssetIdNotFound);
        }

        if (asset is not TAsset tAsset)
        {
            return Result.Failure<TAsset, GetAssetError>(GetAssetError.IncorrectAssetType);
        }

        return Result.Success<TAsset, GetAssetError>(tAsset);
    }
}
