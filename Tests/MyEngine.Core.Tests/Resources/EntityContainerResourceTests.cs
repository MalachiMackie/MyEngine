using MyEngine.Core.Ecs.Resources;

namespace MyEngine.Core.Tests.Resources;

public class EntityContainerResourceTests
{
    private readonly EntityContainerResource _entityContainerResource = new();

    [Fact]
    public void AddEntity_Should_AddEntitiesInCorrectOrder()
    {
        var entity1 = EntityId.Generate();
        var entity2 = EntityId.Generate();

        _entityContainerResource.AddEntity(entity1);
        _entityContainerResource.AddEntity(entity2);

        _entityContainerResource.NewEntities.Should().BeEquivalentTo(new[] {
            entity1,
            entity2
        });
    }

    [Fact]
    public void RemoveEntity_Should_RemoveEntitiesInCorrectOrder()
    {
        var entity1 = EntityId.Generate();
        var entity2 = EntityId.Generate();

        _entityContainerResource.RemoveEntity(entity1);
        _entityContainerResource.RemoveEntity(entity2);

        _entityContainerResource.DeleteEntities.Should().BeEquivalentTo(new[] {
            entity1,
            entity2
        });
    }
}
