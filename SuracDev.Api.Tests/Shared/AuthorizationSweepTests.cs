using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// Deny by default, proven: every endpoint answers 401 to an anonymous request unless it is on the
/// explicit allow list below, which mirrors docs/security-and-privacy.md. A new endpoint fails
/// closed, and opening one is a visible change to this list.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AuthorizationSweepTests(PostgresFixture postgres)
{
  private static readonly HashSet<string> PublicEndpoints = new(StringComparer.OrdinalIgnoreCase)
  {
    "GET api/health",
    "GET api/auth/state",
    "POST api/auth/login",
    "GET api/public/profile",
    "GET api/public/experiences",
    "GET api/public/education",
    "GET api/public/projects",
    "GET api/public/projects/{slug}",
    "GET api/public/skills",
    "GET api/public/legal/{key}",
    "GET api/public/documents/{id}",
    "GET api/public/cv.pdf",
    "GET api/links/{token}",
    "GET api/links/{token}/cv.pdf",
    "GET api/links/{token}/documents/{id}",
  };

  [PostgresFact]
  public async Task Every_endpoint_outside_the_allow_list_rejects_anonymous_requests()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    using var client = factory.CreateClient();
    var endpoints = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
      .ApiDescriptionGroups.Items
      .SelectMany(group => group.Items)
      .Select(description => (Method: description.HttpMethod ?? "GET", Template: description.RelativePath ?? string.Empty))
      .Distinct()
      .ToList();

    Assert.NotEmpty(endpoints);

    var open = new List<string>();
    foreach (var (method, template) in endpoints)
    {
      var key = $"{method} {template}";
      if (PublicEndpoints.Contains(key))
      {
        continue;
      }

      using var request = new HttpRequestMessage(new HttpMethod(method), "/" + WithPlaceholderValues(template))
      {
        Content = new StringContent("{}", Encoding.UTF8, "application/json"),
      };
      using var response = await client.SendAsync(request);
      if (response.StatusCode != HttpStatusCode.Unauthorized)
      {
        open.Add($"{key} -> {(int)response.StatusCode}");
      }
    }

    Assert.True(open.Count == 0, "Endpoints reachable without login:\n" + string.Join('\n', open));
  }

  [PostgresFact]
  public async Task The_allow_list_only_names_endpoints_that_exist()
  {
    await using var factory = new ApiFactory(await postgres.CreateDatabaseAsync());
    var existing = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
      .ApiDescriptionGroups.Items
      .SelectMany(group => group.Items)
      .Select(description => $"{description.HttpMethod} {description.RelativePath}")
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var stale = PublicEndpoints.Where(entry => !existing.Contains(entry)).ToList();
    Assert.True(stale.Count == 0, "Allow-list entries without an endpoint:\n" + string.Join('\n', stale));
  }

  private static string WithPlaceholderValues(string template)
  {
    var result = new StringBuilder();
    var inPlaceholder = false;
    foreach (var character in template)
    {
      if (character == '{')
      {
        inPlaceholder = true;
        result.Append('1');
        continue;
      }

      if (character == '}')
      {
        inPlaceholder = false;
        continue;
      }

      if (!inPlaceholder)
      {
        result.Append(character);
      }
    }

    return result.ToString();
  }
}
