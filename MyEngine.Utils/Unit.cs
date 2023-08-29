namespace MyEngine.Utils;

public sealed class Unit
{
    public static readonly Unit Value = new();

    private Unit()
    {

    }

    public override string? ToString()
    {
        return "<Unit>";
    }
}
