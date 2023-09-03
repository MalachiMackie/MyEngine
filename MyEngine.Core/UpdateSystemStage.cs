namespace MyEngine.Core;

public class UpdateSystemStage : ISystemStage
{
    public static UpdateSystemStage Instance { get; } = new UpdateSystemStage();

    private UpdateSystemStage()
    {

    }

    public bool Equals(ISystemStage? other)
    {
        return other is UpdateSystemStage;
    }
}

public class PostUpdateSystemStage : ISystemStage
{
    public static PostUpdateSystemStage Instance { get; } = new PostUpdateSystemStage();

    private PostUpdateSystemStage() { }

    public bool Equals(ISystemStage? other)
    {
        return other is PostUpdateSystemStage;
    }
}
