namespace HalOS.BuildingBlocks.Domain;

/// <summary>
/// A <see cref="Result"/> that carries a value on success. Accessing <see cref="Value"/>
/// on a failed result throws, so always check <see cref="Result.IsSuccess"/> first.
/// </summary>
/// <typeparam name="TValue">Type of the successful value.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue _value;

    protected internal Result(TValue value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);

    public static new Result<TValue> Failure(Error error) => new(default!, false, error);

    /// <summary>Implicitly wraps a value into a successful result.</summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
