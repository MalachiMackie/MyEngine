using FluentAssertions;

namespace MyEngine.Assets.Tests;

public class AssetCollectionTests
{
    private readonly AssetCollection _assetCollection = new AssetCollection();

    private readonly MyAsset _asset = new();

    public AssetCollectionTests()
    {
        _assetCollection.AddAsset(_asset);
    }

    [Fact]
    public void TryGetAsset_Should_ReturnFoundAsset()
    {
        var result = _assetCollection.TryGetAsset<MyAsset>(_asset.Id);
        result.IsSuccess.Should().BeTrue();
        result.Unwrap().Should().BeSameAs(_asset);
    }

    [Fact]
    public void TryGetAsset_Should_ReturnNull_When_AssetIsNotFound()
    {
        var result = _assetCollection.TryGetAsset<MyAsset>(AssetId.Generate());
        result.IsSuccess.Should().BeTrue();
        result.Unwrap().Should().BeNull();
    }

    [Fact]
    public void TryGetAsset_Should_ReturnError_When_AssetIsIncorrectType()
    {
        var result = _assetCollection.TryGetAsset<OtherAsset>(_asset.Id);
        result.IsFailure.Should().BeTrue();
        result.UnwrapError().Should().Be(IAssetCollection.GetAssetError.IncorrectAssetType);
    }

    [Fact]
    public void AddAsset_Should_AddAssetCorrectly()
    {
        var asset = new MyAsset();
        var result = _assetCollection.AddAsset(asset);
        result.IsSuccess.Should().BeTrue();
        _assetCollection.TryGetAsset<MyAsset>(asset.Id).Unwrap().Should().BeSameAs(asset);
    }

    [Fact]
    public void AddAsset_Should_ReturnError_When_AssetAlreadyExists()
    {
        var result = _assetCollection.AddAsset(_asset);
        result.IsFailure.Should().BeTrue();
        result.UnwrapError().Should().Be(IEditableAssetCollection.AddAssetError.AssetIdAlreadyAdded);
    }
}

public class MyAsset : IAsset
{
    public AssetId Id { get; } = AssetId.Generate();
}

public class OtherAsset : IAsset
{
    public AssetId Id { get; } = AssetId.Generate();
}
