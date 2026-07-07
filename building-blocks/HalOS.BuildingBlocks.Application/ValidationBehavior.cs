using FluentValidation;
using HalOS.BuildingBlocks.Domain;
using MediatR;

namespace HalOS.BuildingBlocks.Application;

/// <summary>
/// MediatR pipeline behavior that runs all registered FluentValidation validators for a
/// request before it reaches its handler (docs/07 §5 — validation via pipeline behavior).
///
/// When the request's response is a <see cref="Result"/> or <see cref="Result{T}"/>, a
/// failed result carrying a <see cref="ValidationError"/> is returned instead of throwing,
/// keeping expected validation failures out of the exception path (docs/07 §10). For any
/// other response type a <see cref="ValidationException"/> is thrown.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var validationError = new ValidationError(
            failures
                .Select(f => new Error(f.ErrorCode ?? ValidationError.ValidationErrorCode, f.ErrorMessage))
                .ToList());

        if (TryCreateFailureResult(validationError.ToError(), out var failureResult))
        {
            return failureResult;
        }

        // Response is not a Result type — fall back to the standard FluentValidation exception.
        throw new ValidationException(failures);
    }

    /// <summary>
    /// Builds a failed <see cref="Result"/> / <see cref="Result{T}"/> matching
    /// <typeparamref name="TResponse"/>, so validation failures flow back as data.
    /// </summary>
    private static bool TryCreateFailureResult(Error error, out TResponse response)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            response = (TResponse)(object)Result.Failure(error);
            return true;
        }

        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];

            // Invoke the non-generic Result.Failure<TValue>(Error) factory.
            var failureMethod = typeof(Result)
                .GetMethods()
                .First(m => m is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true })
                .MakeGenericMethod(valueType);

            response = (TResponse)failureMethod.Invoke(null, new object[] { error })!;
            return true;
        }

        response = default!;
        return false;
    }
}
