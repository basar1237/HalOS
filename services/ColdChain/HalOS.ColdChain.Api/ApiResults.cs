using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.ColdChain.Api;

/// <summary>
/// Domain <see cref="Result"/>'ını uygun HTTP yanıtına çevirir. Hata kodları anlamlı (docs/07 §10);
/// mesajlar kullanıcıya Türkçe döner (RFC 7807 ProblemDetails). Finance.ApiResults deseniyle birebir.
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
            "ColdStorageUnit.NotFound" => StatusCodes.Status404NotFound,
            "ColdStorageUnit.InvalidThresholdRange" => StatusCodes.Status422UnprocessableEntity,
            "ColdStorageUnit.InvalidHumidity" => StatusCodes.Status422UnprocessableEntity,
            "ColdStorageUnit.Inactive" => StatusCodes.Status422UnprocessableEntity,
            "ColdStorageUnit.NameRequired" => StatusCodes.Status422UnprocessableEntity,
            "ColdStorageUnit.ReadingIdRequired" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return controller.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: status);
    }
}
