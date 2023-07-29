namespace MyEngine.Core.Ecs.Resources
{
    public class EntityContainerResource : IResource
    {
        internal Queue<Entity> NewEntities { get; } = new();
        internal Queue<EntityId> DeleteEntities { get; } = new();

        public void AddEntity(Entity entity)
        {
            NewEntities.Enqueue(entity);
        }

        public void RemoveEntity(EntityId entity)
        {
            DeleteEntities.Enqueue(entity);
        }
    }
}
