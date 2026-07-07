using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HalOS.Party.Api;

/// <summary>
/// Beklenmeyen istisnaları yakalar, yapısal olarak log'lar ve istemciye RFC 7807
/// <see cref="ProblemDetails"/> döner (docs/07 §10). Beklenen domain hataları zaten
/// <see cref="HalOS.BuildingBlocks.Domain.Result"/> ile taşınır (bkz. <see cref="ApiResults"/>).
/// İç ayrıntı (exception mesajı/stack) istemciye sızmaz.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Beklenmeyen istisna: {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Sunucu hatası",
                Detail = "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin."
            }
        });
    }
}
