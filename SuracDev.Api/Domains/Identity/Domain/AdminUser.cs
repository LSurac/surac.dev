using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Domains.Identity.Domain;

/// <summary>
/// The single maintainer account. Seeded without a password; the first login sets it. The security
/// stamp travels in the auth cookie, so rotating it logs out every other device.
/// </summary>
public sealed class AdminUser
{
  private AdminUser()
  {
  }

  public long Id { get; private set; }

  public string DisplayName { get; private set; } = null!;

  /// <summary>Null until the first successful login sets the password.</summary>
  public string? PasswordHash { get; private set; }

  public Guid SecurityStamp { get; private set; }

  public DateTimeOffset CreatedAt { get; private set; }

  public bool HasPassword => PasswordHash is not null;

  public static AdminUser Create(string displayName, DateTimeOffset now)
  {
    if (string.IsNullOrWhiteSpace(displayName))
    {
      throw new DomainValidationException(nameof(displayName), "Display name is required.");
    }

    return new AdminUser
    {
      DisplayName = displayName.Trim(),
      SecurityStamp = Guid.NewGuid(),
      CreatedAt = now,
    };
  }

  /// <summary>The first login defines the password. Refuses once a password exists.</summary>
  public void SetInitialPassword(string passwordHash)
  {
    if (HasPassword)
    {
      throw new DomainConflictException("The password has already been set.");
    }

    PasswordHash = passwordHash;
  }

  /// <summary>Replaces the password and rotates the security stamp, invalidating all other sessions.</summary>
  public void ChangePassword(string passwordHash)
  {
    PasswordHash = passwordHash;
    RotateSecurityStamp();
  }

  /// <summary>Invalidates every issued cookie without touching the password.</summary>
  public void RotateSecurityStamp() => SecurityStamp = Guid.NewGuid();

  /// <summary>Stores a rehashed password after a verification that asked for an upgrade. Keeps the stamp.</summary>
  public void UpgradePasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
