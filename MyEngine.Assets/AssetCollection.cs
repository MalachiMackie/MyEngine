using MyEngine.Core.Ecs.Resources;
using MyEngine.Utils;

namespace MyEngine.Assets;

public interface IAssetCollection : IResource
{
    public enum GetAssetError
    {
        IncorrectAssetType
    }

    public Result<TAsset?, GetAssetError> TryGetAsset<TAsset>(AssetId id);
}

internal interface IEditableAssetCollection : IResource
{
    public Result<Unit, AddAssetError> AddAsset(IAsset asset);

    public enum AddAssetError
    {
        AssetIdAlreadyAdded
    }
}

internal class AssetCollection : IAssetCollection, IEditableAssetCollection
{
    private readonly Dictionary<AssetId, IAsset> _assets = new();


    public Result<Unit, IEditableAssetCollection.AddAssetError> AddAsset(IAsset asset)
    {
        if (!_assets.TryAdd(asset.Id, asset))
        {
            return Result.Failure<Unit, IEditableAssetCollection.AddAssetError>(IEditableAssetCollection.AddAssetError.AssetIdAlreadyAdded);
        }

        return Result.Success<Unit, IEditableAssetCollection.AddAssetError>(Unit.Value);
    }


    public Result<TAsset?, IAssetCollection.GetAssetError> TryGetAsset<TAsset>(AssetId id)
    {
        if (!_assets.TryGetValue(id, out var asset))
        {
            return Result.Success<TAsset?, IAssetCollection.GetAssetError>(default);
        }

        if (asset is not TAsset tAsset)
        {
            return Result.Failure<TAsset?, IAssetCollection.GetAssetError>(IAssetCollection.GetAssetError.IncorrectAssetType);
        }

        return Result.Success<TAsset?, IAssetCollection.GetAssetError>(tAsset);
    }
}
