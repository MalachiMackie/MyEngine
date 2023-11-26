using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;

namespace MyEngine.Assets.Tests;

public class AssetCommandsTests
{
    private static readonly string FilePath = "assetPath";
    private static readonly string FileContents = "assetContents";
    private readonly IFileSystem _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { FilePath, new MockFileData(FileContents)}
    });
    private readonly AssetCommands _commands;

    public AssetCommandsTests()
    {
        _commands = new AssetCommands(_fileSystem);
    }

    [Fact]
    public async Task LoadAsset_Should_LoadAssetWithLoadDataCorrectly()
    {
        var assetId = _commands.LoadAsset<MyAsset, string>(FilePath, "myData");

        var commands = _commands.FlushCommands().ToArray();

        var command = commands.Should().ContainSingle().Which.Should().BeOfType<IAssetCommands.LoadAssetCommand>().Subject;
        command.assetId.Should().Be(assetId);

        var commandResult = await command.loadFunc();
        commandResult.Should().BeOfType<MyAsset>()
            .And.BeEquivalentTo(new MyAsset { Id = assetId, Property = "myData", FileContents = FileContents});
    }

    [Fact]
    public async Task LoadAsset_Should_LoadAssetCorrectly()
    {
        var assetId = _commands.LoadAsset<MyAsset>(FilePath);

        var commands = _commands.FlushCommands().ToArray();
        var command = commands.Should().ContainSingle().Which.Should().BeOfType<IAssetCommands.LoadAssetCommand>().Subject;
        command.assetId.Should().Be(assetId);

        var commandResult = await command.loadFunc();
        commandResult.Should().BeOfType<MyAsset>()
            .And.BeEquivalentTo(new MyAsset { Id = assetId, FileContents = FileContents });
    }

    [Fact]
    public void CreateAsset_Should_CreateAssetCorrectly()
    {
        var assetId = _commands.CreateAsset<MyAsset, string>("myData");

        var commands = _commands.FlushCommands().ToArray();
        var command = commands.Should().ContainSingle().Which.Should().BeOfType<IAssetCommands.CreateAssetCommand>().Subject;
        command.assetId.Should().Be(assetId);

        var commandResult = command.createFunc();

        commandResult.Should().BeOfType<MyAsset>()
            .And.BeEquivalentTo(new MyAsset { Id = assetId, Property = "myData" });
    }
}


file class MyAsset : IAsset, ILoadableAsset<MyAsset, string>, ILoadableAsset<MyAsset>, ICreatableAsset<MyAsset, string>
{
    public required AssetId Id { get; init; }

    public string? Property { get; init; }

    public string? FileContents { get; init; }

    public static MyAsset Create(AssetId id, string createData)
    {
        return new MyAsset { Id = id, Property = createData };
    }

    public static async Task<MyAsset> LoadAsync(AssetId id, Stream stream, string loadData)
    {
        var fileContents = await new StreamReader(stream).ReadToEndAsync();
        return new MyAsset() { Id = id, Property = loadData, FileContents = fileContents };
    }

    public static async Task<MyAsset> LoadAsync(AssetId id, Stream stream)
    {
        var fileContents = await new StreamReader(stream).ReadToEndAsync();
        return new MyAsset() { Id = id, FileContents = fileContents };
    }
}
