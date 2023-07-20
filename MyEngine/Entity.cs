namespace MyEngine
{
    internal class Entity
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
}
