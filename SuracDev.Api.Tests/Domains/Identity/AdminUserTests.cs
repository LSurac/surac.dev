using SuracDev.Api.Domains.Identity.Domain;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Tests.Domains.Identity;

public sealed class AdminUserTests
{
  private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public void A_new_admin_has_no_password_and_a_security_stamp()
  {
    var admin = AdminUser.Create("  Leonard ", Now);

    Assert.Equal("Leonard", admin.DisplayName);
    Assert.False(admin.HasPassword);
    Assert.NotEqual(Guid.Empty, admin.SecurityStamp);
    Assert.Equal(Now, admin.CreatedAt);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void A_blank_display_name_is_rejected(string displayName)
  {
    Assert.Throws<DomainValidationException>(() => AdminUser.Create(displayName, Now));
  }

  [Fact]
  public void The_initial_password_can_only_be_set_once()
  {
    var admin = AdminUser.Create("Admin", Now);

    admin.SetInitialPassword("hash-1");

    Assert.True(admin.HasPassword);
    Assert.Throws<DomainConflictException>(() => admin.SetInitialPassword("hash-2"));
    Assert.Equal("hash-1", admin.PasswordHash);
  }

  [Fact]
  public void Changing_the_password_rotates_the_security_stamp()
  {
    var admin = AdminUser.Create("Admin", Now);
    admin.SetInitialPassword("hash-1");
    var stampBefore = admin.SecurityStamp;

    admin.ChangePassword("hash-2");

    Assert.Equal("hash-2", admin.PasswordHash);
    Assert.NotEqual(stampBefore, admin.SecurityStamp);
  }

  [Fact]
  public void Upgrading_the_hash_keeps_the_security_stamp()
  {
    var admin = AdminUser.Create("Admin", Now);
    admin.SetInitialPassword("hash-1");
    var stampBefore = admin.SecurityStamp;

    admin.UpgradePasswordHash("hash-1-rehashed");

    Assert.Equal("hash-1-rehashed", admin.PasswordHash);
    Assert.Equal(stampBefore, admin.SecurityStamp);
  }

  [Theory]
  [InlineData("short")]
  [InlineData("elevenchars")]
  [InlineData("            ")]
  public void Passwords_shorter_than_twelve_characters_are_rejected(string password)
  {
    var exception = Assert.Throws<DomainValidationException>(() => PasswordPolicy.Ensure(password));

    Assert.Contains("password", exception.Errors.Keys);
  }

  [Fact]
  public void Twelve_characters_satisfy_the_policy()
  {
    PasswordPolicy.Ensure("twelve-chars");
  }
}
