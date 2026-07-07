using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Sales.Api;

/// <summary>
/// Domain <see cref="Result"/>'ını uygun HTTP yanıtına çevirir. Hata kodları anlamlı (docs/07 §10);
/// mesajlar kullanıcıya Türkçe döner (RFC 7807 ProblemDetails).
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
            "Sale.NotFound" => StatusCodes.Status404NotFound,
            "Consignment.NotFound" => StatusCodes.Status404NotFound,
            "Sale.AlreadyCompleted" => StatusCodes.Status409Conflict,
            "Sale.AlreadyCancelled" => StatusCodes.Status409Conflict,
            "Sale.CancelledSaleCannotComplete" => StatusCodes.Status409Conflict,
            "Sale.NotDraft" => StatusCodes.Status409Conflict,
            "Sale.NoLines" => StatusCodes.Status409Conflict,
            "Sale.DuplicateOperation" => StatusCodes.Status409Conflict,
            "Settlement.NegativeNet" => StatusCodes.Status422UnprocessableEntity,
            "RateSet.CommissionRateTooHigh" => StatusCodes.Status422UnprocessableEntity,
            "RateSet.NegativeRate" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return controller.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: status);
    }
}
