namespace MyEngine.Core;

public interface ISystemStage : IEquatable<ISystemStage>
{
    public static readonly ISystemStage Update = UpdateSystemStage.Instance;
}
