using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Finance.Api;

/// <summary>
/// Domain <see cref="Result"/>'ını uygun HTTP yanıtına çevirir. Hata kodları anlamlı (docs/07 §10);
/// mesajlar kullanıcıya Türkçe döner (RFC 7807 ProblemDetails). Sales.ApiResults deseniyle birebir.
/// </summary>
public static class ApiResults
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        return result.IsSuccess
            ? controller.Ok(result.Value)
            : Problem(result.Error, controller);
    }

    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        return result.IsSuccess
            ? controller.NoContent()
            : Problem(result.Error, controller);
    }

    private static IActionResult Problem(Error error, ControllerBase controller)
    {
        var status = error.Code switch
        {
            ValidationError.ValidationErrorCode => StatusCodes.Status400BadRequest,
            "CurrentAccount.NotFound" => StatusCodes.Status404NotFound,
            "CurrentAccount.CashLimitExceeded" => StatusCodes.Status422UnprocessableEntity,
            "CurrentAccount.NegativeNet" => StatusCodes.Status422UnprocessableEntity,
            "CurrentAccount.NonPositiveAmount" => StatusCodes.Status422UnprocessableEntity,
            "CurrentAccount.PartyRequired" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return controller.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: status);
    }
}
