namespace HalOS.BuildingBlocks.Domain;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail without throwing.
/// Prefer returning a <see cref="Result"/> for expected domain failures (docs/07 §10);
/// reserve exceptions for truly exceptional conditions.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        // A success must carry no error; a failure must carry one. Guard the invariant.
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) =>
        Result<TValue>.Success(value);

    public static Result<TValue> Failure<TValue>(Error error) =>
        Result<TValue>.Failure(error);
}
