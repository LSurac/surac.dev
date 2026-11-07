using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Domains.Identity.Domain;

/// <summary>
/// The one rule for admin passwords: length. Complexity rules push people towards predictable
/// patterns; twelve characters with the PBKDF2 hasher and the login rate limit is the chosen bar.
/// </summary>
public static class PasswordPolicy
{
  public const int MinimumLength = 12;

  public static void Ensure(string password, string field = "password")
  {
    if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumLength)
    {
      throw new DomainValidationException(field, $"The password needs at least {MinimumLength} characters.");
    }
  }
}
