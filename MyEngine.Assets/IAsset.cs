namespace MyEngine.Assets;

public class AssetId : IEquatable<AssetId>
{
    private Guid Value { get; }

    private AssetId(Guid value)
    {
        Value = value;
    }

    public static AssetId Generate() => new(Guid.NewGuid());

    public bool Equals(AssetId? other)
    {
        return other != null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as AssetId);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}

public interface IAsset
{
    AssetId Id { get; }

}

public interface ILoadableAsset<TAsset, TLoadData> : IAsset
    where TAsset : IAsset
{
    static abstract Task<TAsset> LoadAsync(AssetId id, Stream stream, TLoadData loadData); 
}

public interface ILoadableAsset<TAsset> : IAsset
    where TAsset : IAsset
{
    static abstract Task<TAsset> LoadAsync(AssetId id, Stream stream);
}

public interface ICreatableAsset<TAsset, TCreateData> : IAsset
    where TAsset : IAsset
{
    static abstract TAsset Create(AssetId id, TCreateData createData);
}

