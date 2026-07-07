using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Party.Api;

/// <summary>
/// Domain <see cref="Result"/>'ını uygun HTTP yanıtına çevirir. Hata kodları anlamlı
/// (docs/07 §10); mesajlar kullanıcıya Türkçe döner (RFC 7807 ProblemDetails).
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
            "Party.NotFound" => StatusCodes.Status404NotFound,
            "Party.TcknAlreadyInUse" => StatusCodes.Status409Conflict,
            "Party.VknAlreadyInUse" => StatusCodes.Status409Conflict,
            "Party.RoleAlreadyExists" => StatusCodes.Status409Conflict,
            "Party.AlreadyInactive" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return controller.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: status);
    }
}
