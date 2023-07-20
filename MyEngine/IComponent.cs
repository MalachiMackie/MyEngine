namespace MyEngine
{
    internal interface IComponent
    {
        public EntityId EntityId { get; }

        public static abstract bool AllowMultiple { get; } 
    }
}
