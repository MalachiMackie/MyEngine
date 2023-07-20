namespace MyEngine
{
    internal class EntityId
    {
        // todo: determine if there's a better id value
        public Guid Value { get; init; }

        public static EntityId Generate()
        {
            return new EntityId { Value = Guid.NewGuid() };
        }
    }
}
