namespace MyEngine.Core.Ecs
{
    public class Entity
    {
        public Entity()
        {
            Id = EntityId.Generate();
        }

        public Entity(EntityId id)
        {
            Id = id;
        }

        public EntityId Id { get; }
    }

    public class EntityId
    {
        // todo: determine if there's a better id value
        public Guid Value { get; init; }

        public static EntityId Generate()
        {
            return new EntityId { Value = Guid.NewGuid() };
        }
    }
}
