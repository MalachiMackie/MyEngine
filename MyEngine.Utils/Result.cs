using System.Diagnostics.CodeAnalysis;

namespace MyEngine.Utils;


[Serializable]
public class FailedResultValueRetrievalException : Exception
{
    public FailedResultValueRetrievalException() { }
    public FailedResultValueRetrievalException(string message) : base(message) { }
    public FailedResultValueRetrievalException(string message, Exception inner) : base(message, inner) { }
    protected FailedResultValueRetrievalException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}


[Serializable]
public class SuccessfulResultErrorRetrievalException : Exception
{
    public SuccessfulResultErrorRetrievalException() { }
    public SuccessfulResultErrorRetrievalException(string message) : base(message) { }
    public SuccessfulResultErrorRetrievalException(string message, Exception inner) : base(message, inner) { }
    protected SuccessfulResultErrorRetrievalException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(params string[] errors) => Result<T>.Failure(errors);
    public static Result<T> Failure<T>(IEnumerable<string> errors) => Result<T>.Failure(errors as string[] ?? errors.ToArray());

    // todo: try and get rid of T2
    public static Result<T> Failure<T, T2>(Result<T2> failedResult)
    {
        if (!failedResult.TryGetErrors(out var errors))
        {
            throw new InvalidOperationException("Tried to create a failed result from a successful one");
        }

        return Failure<T>(errors);
    }
}

public readonly struct Result<T>
{
    [MemberNotNullWhen(true, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_errors))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(false, nameof(_value))]
    [MemberNotNullWhen(true, nameof(_errors))]
    public bool IsFailure => !IsSuccess;

    private readonly T? _value;

    private readonly string[]? _errors;

    private Result(T? value, string[]? errors, bool isSuccess)
    {
        _value = value;
        _errors = errors;
        IsSuccess = isSuccess;
    }

    public T Unwrap()
    {
        return IsFailure
            ? throw new FailedResultValueRetrievalException()
            : _value;
    }

    public T Expect(string because)
    {
        return IsFailure
            ? throw new FailedResultValueRetrievalException($"""Expected result to be successful because "{because}", but it was a failed result""")
            : _value;
    }

    public IEnumerable<string> GetErrors()
    {
        return IsSuccess
            ? throw new SuccessfulResultErrorRetrievalException()
            : _errors;
    }

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = _value;
        return IsSuccess;
    }

    public bool TryGetErrors([NotNullWhen(true)] out IEnumerable<string>? errors)
    {
        errors = _errors;
        return IsFailure;
    }

    public Result<TMapped> Map<TMapped>(Func<T, TMapped> mapFunc)
    {
        if (IsSuccess)
        {
            return Result<TMapped>.Success(mapFunc(_value));
        }

        return Result<TMapped>.Failure(_errors);
    }

    public static Result<T> Success(T value) => new(value, default, true);

    public static Result<T> Failure(params string[] errors) => new(default, errors, false);

    public static implicit operator Result<Unit>(Result<T> result)
    {
        return result.Map(_ => Unit.Value);
    }
}
