using System.Net;
using System.Net.Http.Json;
using SuracDev.Api.Domains.Identity.Contracts;
using SuracDev.Api.Tests.Shared;

namespace SuracDev.Api.Tests.Domains.Identity;

[Collection(PostgresCollection.Name)]
public sealed class AuthEndpointTests(PostgresFixture postgres)
{
  [PostgresFact]
  public async Task The_seeded_admin_starts_without_a_password()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var state = await client.GetFromJsonAsync<LoginStateResponse>("/api/auth/state");

    Assert.NotNull(state);
    Assert.False(state.HasPassword);
  }

  [PostgresFact]
  public async Task The_first_login_sets_the_password_and_opens_a_session()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(ApiFactory.InitialPassword));
    var session = await response.Content.ReadFromJsonAsync<SessionResponse>();
    var me = await client.GetAsync("/api/auth/me");
    var state = await client.GetFromJsonAsync<LoginStateResponse>("/api/auth/state");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.NotNull(session);
    Assert.True(session.IsInitialLogin);
    Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    Assert.True(state!.HasPassword);
  }

  [PostgresFact]
  public async Task A_short_first_password_is_rejected_and_nothing_is_stored()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("short"));
    var state = await client.GetFromJsonAsync<LoginStateResponse>("/api/auth/state");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.False(state!.HasPassword);
  }

  [PostgresFact]
  public async Task A_wrong_password_is_a_validation_error_not_a_session()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using (var first = await factory.CreateLoggedInClientAsync())
    {
      first.Dispose();
    }

    using var client = factory.CreateClient();
    var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("definitely not the password"));
    var me = await client.GetAsync("/api/auth/me");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
  }

  [PostgresFact]
  public async Task Anonymous_requests_to_the_session_are_unauthorized()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    var me = await client.GetAsync("/api/auth/me");

    Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
  }

  [PostgresFact]
  public async Task Changing_the_password_logs_out_other_devices_but_not_this_one()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var laptop = await factory.CreateLoggedInClientAsync();
    using var phone = await factory.CreateLoggedInClientAsync();

    var change = await laptop.PostAsJsonAsync(
      "/api/auth/change-password",
      new ChangePasswordRequest(ApiFactory.InitialPassword, "a brand new passphrase"));
    var laptopStillIn = await laptop.GetAsync("/api/auth/me");
    var phoneKickedOut = await phone.GetAsync("/api/auth/me");

    Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
    Assert.Equal(HttpStatusCode.OK, laptopStillIn.StatusCode);
    Assert.Equal(HttpStatusCode.Unauthorized, phoneKickedOut.StatusCode);
  }

  [PostgresFact]
  public async Task Changing_the_password_requires_the_current_one()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = await factory.CreateLoggedInClientAsync();

    var change = await client.PostAsJsonAsync(
      "/api/auth/change-password",
      new ChangePasswordRequest("not the current password", "a brand new passphrase"));

    Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
  }

  [PostgresFact]
  public async Task Logout_ends_the_session()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = await factory.CreateLoggedInClientAsync();

    var logout = await client.PostAsync("/api/auth/logout", content: null);
    var me = await client.GetAsync("/api/auth/me");

    Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
  }

  [PostgresFact]
  public async Task Repeated_failed_logins_are_rate_limited()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    HttpStatusCode last = default;
    for (var attempt = 0; attempt < 11; attempt++)
    {
      using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("short"));
      last = response.StatusCode;
    }

    Assert.Equal(HttpStatusCode.TooManyRequests, last);
  }
}
