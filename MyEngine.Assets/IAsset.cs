namespace MyEngine.Assets;

public class AssetId
{
    private Guid Value { get; }

    private AssetId(Guid value)
    {
        Value = value;
    }

    internal static AssetId Generate() => new AssetId(Guid.NewGuid());
}

public interface IAsset
{
    AssetId Id { get; }

}

public interface ILoadableAsset : IAsset
{
    static abstract Task<IAsset> LoadAsync(AssetId id, Stream stream); 
}
