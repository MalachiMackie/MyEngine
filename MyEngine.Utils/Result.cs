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
    public static Result<T, TError> Success<T, TError>(T value) => Result<T, TError>.Success(value);
    public static Result<T, TError> Failure<T, TError>(TError error) => Result<T, TError>.Failure(error);
}

public readonly struct Result<T, TError>
{
    [MemberNotNullWhen(true, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(false, nameof(_value))]
    [MemberNotNullWhen(true, nameof(_error))]
    public bool IsFailure => !IsSuccess;

    private readonly T? _value;

    private readonly TError? _error;

    private Result(T? value, TError? error, bool isSuccess)
    {
        _value = value;
        _error = error;
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

    public TError UnwrapError()
    {
        return IsSuccess
            ? throw new SuccessfulResultErrorRetrievalException()
            : _error;
    }

    public bool TryGetError([NotNullWhen(true)] out TError? error)
    {
        error = _error;
        return IsFailure;
    }

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = _value;
        return IsSuccess;
    }

    public Result<TMapped, TError> Map<TMapped>(Func<T, TMapped> mapFunc)
    {
        if (IsSuccess)
        {
            return Result<TMapped, TError>.Success(mapFunc(_value));
        }

        return Result<TMapped, TError>.Failure(_error);
    }

    public Result<T, TMappedError> MapError<TMappedError>(Func<TError, TMappedError> mapErrorFunc)
    {
        if (IsFailure)
        {
            return Result<T, TMappedError>.Failure(mapErrorFunc(_error));
        }

        return Result<T, TMappedError>.Success(_value);
    }

    public static Result<T, TError> Success(T value) => new(value, default, true);

    public static Result<T, TError> Failure(TError error) => new(default, error, false);

    public void Match(Action<T> onSuccess, Action<TError> onError)
    {
        if (IsSuccess)
        {
            onSuccess(_value);
        }
        else
        {
            onError(_error);
        }
    }
}
