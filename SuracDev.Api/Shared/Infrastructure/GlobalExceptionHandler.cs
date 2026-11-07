using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Shared.Infrastructure;

/// <summary>
/// Translates the <see cref="DomainException"/> hierarchy into ProblemDetails so services simply
/// throw and controllers never need try/catch. Anything else falls through to the default handling.
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken)
  {
    ProblemDetails? problemDetails = exception switch
    {
      DomainValidationException validation => new ValidationProblemDetails(ToModelState(validation.Errors))
      {
        Status = StatusCodes.Status400BadRequest,
        Title = "Validation failed.",
      },
      DomainConflictException conflict => new ProblemDetails
      {
        Status = StatusCodes.Status409Conflict,
        Title = "Conflict.",
        Detail = conflict.Message,
      },
      DomainNotFoundException notFound => new ProblemDetails
      {
        Status = StatusCodes.Status404NotFound,
        Title = "Not found.",
        Detail = notFound.Message,
      },
      _ => null,
    };

    if (problemDetails is null)
    {
      return false;
    }

    httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

    return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
      HttpContext = httpContext,
      Exception = exception,
      ProblemDetails = problemDetails,
    });
  }

  private static ModelStateDictionary ToModelState(IReadOnlyDictionary<string, string[]> errors)
  {
    var modelState = new ModelStateDictionary();
    foreach (var (field, messages) in errors)
    {
      foreach (var message in messages)
      {
        modelState.AddModelError(field, message);
      }
    }

    return modelState;
  }
}
