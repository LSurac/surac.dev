using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Shared.Infrastructure;

/// <summary>
/// Translates the <see cref="DomainException"/> hierarchy into ProblemDetails so services simply
/// throw and controllers never need try/catch. Unique and foreign-key violations that slip past the
/// services' own checks (two concurrent saves) become 409 without database details. Anything else
/// falls through to the default handling: a logged 500 without internals.
/// </summary>
public sealed class GlobalExceptionHandler(
  IProblemDetailsService problemDetailsService,
  ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
  /// <summary>Maps an expected exception to ProblemDetails; returns false for everything else.</summary>
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
      DbUpdateException { InnerException: PostgresException postgres } when IsConstraintViolation(postgres)
        => ConstraintConflict(postgres),
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

  private static bool IsConstraintViolation(PostgresException exception)
    => exception.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation;

  private ProblemDetails ConstraintConflict(PostgresException exception)
  {
    // The constraint name helps the maintainer in the log; the client only learns that it was a conflict.
    logger.LogWarning(
      "Database constraint {Constraint} rejected a save ({SqlState}).", exception.ConstraintName, exception.SqlState);
    return new ProblemDetails
    {
      Status = StatusCodes.Status409Conflict,
      Title = "Conflict.",
      Detail = exception.SqlState == PostgresErrorCodes.UniqueViolation
        ? "An entry with the same unique value already exists."
        : "The entry references something that does not exist or is still referenced elsewhere.",
    };
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
