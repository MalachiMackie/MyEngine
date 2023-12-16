using FakeItEasy;
using FluentAssertions;

namespace MyEngine.Assets.Tests;

public class AssetLoadingSystemTests
{
    private readonly IAssetCollection _assetCollection = A.Fake<IAssetCollection>();
    private readonly IAssetCommands _assetCommands = A.Fake<IAssetCommands>();

    private readonly AssetLoadingSystem _system;

    public AssetLoadingSystemTests()
    {
        _system = new(_assetCommands, _assetCollection);
    }

    [Fact]
    public async void LoadAssets_Should_LoadAllCurrentAssets()
    {
        var asset1 = new AssetLoadingSystemAsset();
        var asset2 = new AssetLoadingSystemAsset();

        var taskCompletionSource = new TaskCompletionSource();

        A.CallTo(() => _assetCommands.FlushCommands())
            .Returns(new[] { new IAssetCommands.CreateAssetCommand(asset1.Id, () => asset1) })
            .Once()
            .Then.Returns(Array.Empty<IAssetCommands.IAssetCommand>())
            .Once()
            .Then.Returns(new[] { new IAssetCommands.LoadAssetCommand(asset2.Id, () => {
                return Task.FromResult<IAsset>(asset2);
            }) });

        _system.Run(1);

        A.CallTo(() => _assetCollection.AddAsset(An<AssetLoadingSystemAsset>.That.Matches(x => x.Id == asset1.Id)))
            .MustHaveHappened();

        _system.Run(1);

        A.CallTo(() => _assetCollection.AddAsset(An<AssetLoadingSystemAsset>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _assetCollection.AddAsset(An<AssetLoadingSystemAsset>.That.Matches(x => x.Id == asset2.Id)))
            .Invokes(taskCompletionSource.SetResult);

        _system.Run(1);

        await taskCompletionSource.Should().CompleteWithinAsync(TimeSpan.FromMilliseconds(100));
    }
}

public class AssetLoadingSystemAsset : IAsset
{
    public AssetId Id { get; } = AssetId.Generate();
}
