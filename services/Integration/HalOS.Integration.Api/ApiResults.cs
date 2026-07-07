using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Integration.Api;

/// <summary>
/// Domain <see cref="Result"/>'ını uygun HTTP yanıtına çevirir. Hata kodları anlamlı (docs/07 §10);
/// mesajlar kullanıcıya Türkçe döner (RFC 7807 ProblemDetails). Finance/Sales.ApiResults deseniyle birebir.
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
            "ProducerReceipt.NotFound" => StatusCodes.Status404NotFound,
            "ProducerReceipt.NonPositiveGross" => StatusCodes.Status422UnprocessableEntity,
            "ProducerReceipt.NegativeDeduction" => StatusCodes.Status422UnprocessableEntity,
            "ProducerReceipt.NegativeNet" => StatusCodes.Status422UnprocessableEntity,
            "ProducerReceipt.ReceiptNumberRequired" => StatusCodes.Status422UnprocessableEntity,
            "ProducerReceipt.CancelledCannotIssue" => StatusCodes.Status422UnprocessableEntity,
            "ProducerReceipt.SaleRequired" => StatusCodes.Status422UnprocessableEntity,
            "ProducerReceipt.ProducerRequired" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return controller.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: status);
    }
}
