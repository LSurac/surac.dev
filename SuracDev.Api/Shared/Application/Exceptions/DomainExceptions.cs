namespace SuracDev.Api.Shared.Application.Exceptions;

/// <summary>
/// Base type for expected domain errors, mapped to HTTP responses centrally. Closed, so a new kind of
/// domain error does not compile until <c>GlobalExceptionHandler</c> maps it to a status.
/// </summary>
public closed class DomainException(string message) : Exception(message);

/// <summary>A validation error with per-field messages (mapped to 400 ValidationProblemDetails).</summary>
public sealed class DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
  : DomainException("Validation failed.")
{
  public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

  public DomainValidationException(string field, string error)
    : this(new Dictionary<string, string[]> { [field] = [error] })
  {
  }
}

/// <summary>A conflict with the current state of a resource (mapped to 409).</summary>
public sealed class DomainConflictException(string message) : DomainException(message);

/// <summary>A referenced resource does not exist (mapped to 404).</summary>
public sealed class DomainNotFoundException(string message) : DomainException(message);
