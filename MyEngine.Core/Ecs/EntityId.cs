namespace MyEngine.Core.Ecs;

public class EntityId
{
    private EntityId()
    {

    }

    // todo: determine if there's a better id value
    public required Guid Value { get; init; }

    public static EntityId Generate()
    {
        return new EntityId { Value = Guid.NewGuid() };
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
