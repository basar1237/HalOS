using HalOS.BuildingBlocks.Domain;

namespace HalOS.BuildingBlocks.Application;

/// <summary>
/// A <see cref="Error"/> that aggregates one or more FluentValidation field failures.
/// Produced by <see cref="ValidationBehavior{TRequest,TResponse}"/> when a request fails
/// validation, so handlers/API can surface field-level messages to the user.
/// </summary>
public sealed record ValidationError : IEquatable<ValidationError>
{
    public const string ValidationErrorCode = "Validation.Failed";

    public ValidationError(IReadOnlyList<Error> errors)
    {
        Errors = errors;
    }

    /// <summary>Individual field failures.</summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>The aggregate error used to fail a <see cref="Result"/>.</summary>
    public Error ToError()
    {
        var message = string.Join("; ", Errors.Select(e => e.Message));
        return new Error(ValidationErrorCode, message);
    }
}
