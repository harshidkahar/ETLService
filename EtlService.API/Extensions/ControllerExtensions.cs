using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace EtlService.API.Extensions;

public static class ControllerExtensions
{
    public static IActionResult Problem(this ControllerBase controller, ErrorOr.Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        return controller.Problem(title: error.Code, detail: error.Description, statusCode: statusCode);
    }
}
