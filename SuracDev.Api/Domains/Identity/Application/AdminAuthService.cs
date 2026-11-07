using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Identity.Domain;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Domains.Identity.Application;

/// <summary>
/// Login and password handling for the single admin. The first login sets the password; later
/// logins verify it. Hashing is ASP.NET Core Identity's PBKDF2 hasher without the rest of Identity.
/// </summary>
public sealed class AdminAuthService(SuracDevDbContext dbContext)
{
  private readonly PasswordHasher<AdminUser> passwordHasher = new();

  public async Task<AdminUser> GetAdminAsync(CancellationToken cancellationToken)
    => await dbContext.AdminUsers.OrderBy(user => user.Id).FirstOrDefaultAsync(cancellationToken)
      ?? throw new DomainNotFoundException("No admin user exists.");

  public async Task<AdminUser> GetByIdAsync(long id, CancellationToken cancellationToken)
    => await dbContext.AdminUsers.FindAsync([id], cancellationToken)
      ?? throw new DomainNotFoundException("Admin user not found.");

  /// <summary>Logs the admin in. Sets the password on the first attempt, verifies it afterwards.</summary>
  public async Task<AdminUser> LoginAsync(string password, CancellationToken cancellationToken)
  {
    var admin = await GetAdminAsync(cancellationToken);
    PasswordPolicy.Ensure(password);

    if (!admin.HasPassword)
    {
      admin.SetInitialPassword(passwordHasher.HashPassword(admin, password));
      await dbContext.SaveChangesAsync(cancellationToken);
      return admin;
    }

    var result = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash!, password);
    if (result == PasswordVerificationResult.Failed)
    {
      throw new DomainValidationException("password", "Wrong password.");
    }

    if (result == PasswordVerificationResult.SuccessRehashNeeded)
    {
      admin.UpgradePasswordHash(passwordHasher.HashPassword(admin, password));
      await dbContext.SaveChangesAsync(cancellationToken);
    }

    return admin;
  }

  /// <summary>Changes the password; the rotated security stamp logs out every other device.</summary>
  public async Task<AdminUser> ChangePasswordAsync(
    long adminId,
    string currentPassword,
    string newPassword,
    CancellationToken cancellationToken)
  {
    var admin = await GetByIdAsync(adminId, cancellationToken);
    PasswordPolicy.Ensure(newPassword, "newPassword");

    if (!admin.HasPassword)
    {
      throw new DomainConflictException("Set the password through the first login before changing it.");
    }

    var result = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash!, currentPassword);
    if (result == PasswordVerificationResult.Failed)
    {
      throw new DomainValidationException("currentPassword", "The current password is wrong.");
    }

    admin.ChangePassword(passwordHasher.HashPassword(admin, newPassword));
    await dbContext.SaveChangesAsync(cancellationToken);
    return admin;
  }
}
