namespace MyEngine.Core.Ecs.Components
{
    public class PhysicsMaterial : IComponent
    {
        public float Bounciness { get; set; }

        public PhysicsMaterial(float bounciness)
        {
            Bounciness = Math.Clamp(bounciness, 0f, 1f);
        }

    }
}
