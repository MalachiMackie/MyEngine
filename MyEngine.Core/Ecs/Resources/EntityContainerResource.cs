namespace MyEngine.Core.Ecs.Resources
{
    internal class EntityContainerResource : IResource
    {
        internal Queue<Entity> NewEntities { get; } = new();

        public void AddEntity(Entity entity)
        {
            NewEntities.Enqueue(entity);
        }
    }
}
