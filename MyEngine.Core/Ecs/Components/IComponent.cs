using MyEngine.Core.Ecs;

namespace MyEngine.Core.Ecs.Components
{
    public interface IComponent
    {
        public EntityId EntityId { get; }

        public static abstract bool AllowMultiple { get; }
    }
}
