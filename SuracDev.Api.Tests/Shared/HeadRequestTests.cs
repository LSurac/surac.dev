using System.Net;

namespace SuracDev.Api.Tests.Shared;

[Collection(PostgresCollection.Name)]
public sealed class HeadRequestTests(PostgresFixture postgres)
{
  [PostgresFact]
  public async Task Public_resources_answer_head_like_get()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();
    (string Path, string ContentType)[] resources =
    [
      ("/api/health", "application/json"),
      ("/api/public/profile", "application/json"),
      ("/api/public/experiences", "application/json"),
      ("/api/public/cv.pdf", "application/pdf"),
    ];

    foreach (var (path, contentType) in resources)
    {
      using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, path));

      Assert.True(response.StatusCode == HttpStatusCode.OK, $"HEAD {path} -> {(int)response.StatusCode}");
      Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
    }
  }

  [PostgresFact]
  public async Task Admin_resources_still_reject_anonymous_head_requests()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/api/admin/profile"));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [PostgresFact]
  public async Task Application_links_do_not_answer_head_so_it_never_counts_as_an_open()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();

    using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/api/links/any-token"));

    Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
  }
}
