using System.Diagnostics;

namespace MyEngine.Utils;
public readonly struct OneOf<T1, T2>
{
    private enum PresentValue
    {
        T1,
        T2
    }

    private readonly PresentValue _presentValue;

    private readonly T1? _value1;
    private readonly T2? _value2;

    public OneOf(T1 value)
    {
        _value1 = value;
        _presentValue = PresentValue.T1;
    }

    public OneOf(T2 value)
    {
        _value2 = value;
        _presentValue = PresentValue.T2;
    }

    public void Match(Action<T1> matchValue1, Action<T2> matchValue2)
    {
        switch (_presentValue)
        {
            case PresentValue.T1:
                matchValue1(_value1!);
                break;
            case PresentValue.T2:
                matchValue2(_value2!);
                break;
            default:
                throw new UnreachableException();
        }
    }

    public TOutput Match<TOutput>(Func<T1, TOutput> matchValue1, Func<T2, TOutput> matchValue2)
    {
        return _presentValue switch
        {
            PresentValue.T1 => matchValue1(_value1!),
            PresentValue.T2 => matchValue2(_value2!),
            _ => throw new UnreachableException()
        };
    }
}

public readonly struct OneOf<T1, T2, T3>
{
    private enum PresentValue
    {
        T1,
        T2,
        T3,
    }

    private readonly PresentValue _presentValue;

    private readonly T1? _value1;
    private readonly T2? _value2;
    private readonly T3? _value3;

    public OneOf(T1 value)
    {
        _value1 = value;
        _presentValue = PresentValue.T1;
    }

    public OneOf(T2 value)
    {
        _value2 = value;
        _presentValue = PresentValue.T2;
    }

    public OneOf(T3 value)
    {
        _value3 = value;
        _presentValue = PresentValue.T3;
    }

    public void Match(Action<T1> matchValue1, Action<T2> matchValue2, Action<T3> matchValue3)
    {
        switch (_presentValue)
        {
            case PresentValue.T1: matchValue1(_value1!); break;
            case PresentValue.T2: matchValue2(_value2!); break;
            case PresentValue.T3: matchValue3(_value3!); break;
            default:
                throw new UnreachableException();
        }
    }

    public TOutput Match<TOutput>(Func<T1, TOutput> matchValue1, Func<T2, TOutput> matchValue2, Func<T3, TOutput> matchValue3)
    {
        return _presentValue switch
        {
            PresentValue.T1 => matchValue1(_value1!),
            PresentValue.T2 => matchValue2(_value2!),
            PresentValue.T3 => matchValue3(_value3!),
            _ => throw new UnreachableException()
        };
    }
}

public readonly struct OneOf<T1, T2, T3, T4>
{
    private enum PresentValue
    {
        T1,
        T2,
        T3,
        T4,
    }

    private readonly PresentValue _presentValue;

    private readonly T1? _value1;
    private readonly T2? _value2;
    private readonly T3? _value3;
    private readonly T4? _value4;

    public OneOf(T1 value)
    {
        _value1 = value;
        _presentValue = PresentValue.T1;
    }

    public OneOf(T2 value)
    {
        _value2 = value;
        _presentValue = PresentValue.T2;
    }

    public OneOf(T3 value)
    {
        _value3 = value;
        _presentValue = PresentValue.T3;
    }

    public OneOf(T4 value)
    {
        _value4 = value;
        _presentValue = PresentValue.T4;
    }

    public void Match(Action<T1> matchValue1, Action<T2> matchValue2, Action<T3> matchValue3, Action<T4> matchValue4)
    {
        switch (_presentValue)
        {
            case PresentValue.T1: matchValue1(_value1!); break;
            case PresentValue.T2: matchValue2(_value2!); break;
            case PresentValue.T3: matchValue3(_value3!); break;
            case PresentValue.T4: matchValue4(_value4!); break;
            default:
                throw new UnreachableException();
        }
    }

    public TOutput Match<TOutput>(Func<T1, TOutput> matchValue1, Func<T2, TOutput> matchValue2, Func<T3, TOutput> matchValue3, Func<T4, TOutput> matchValue4)
    {
        return _presentValue switch
        {
            PresentValue.T1 => matchValue1(_value1!),
            PresentValue.T2 => matchValue2(_value2!),
            PresentValue.T3 => matchValue3(_value3!),
            PresentValue.T4 => matchValue4(_value4!),
            _ => throw new UnreachableException()
        };
    }
}
