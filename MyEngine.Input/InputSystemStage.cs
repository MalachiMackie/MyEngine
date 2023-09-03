using MyEngine.Core;

namespace MyEngine.Input;

public class InputSystemStage : ISystemStage
{
    public static InputSystemStage Instance { get; } = new InputSystemStage();

    private InputSystemStage()
    {
    }

    public bool Equals(ISystemStage? other)
    {
        return other is InputSystemStage;
    }
}
