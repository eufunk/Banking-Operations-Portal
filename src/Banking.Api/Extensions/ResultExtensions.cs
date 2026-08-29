using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Extensions;

/// <summary>
/// Einzige Stelle, an der ein Application-Result in eine HTTP-Antwort übersetzt wird.
/// Hält Controller dünn: kein Controller entscheidet selbst, welcher Errortyp zu
/// welchem Statuscode gehört - das steht genau einmal hier.
/// </summary>
public static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : result.Error!.ToProblemResult<T>();
    }

    public static ActionResult<T> ToProblemResult<T>(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Message,
            Type = $"https://httpstatuses.io/{statusCode}",
        };

        problemDetails.Extensions["code"] = error.Code;

        if (error.Details is { Count: > 0 })
        {
            problemDetails.Extensions["errors"] = error.Details;
        }

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
