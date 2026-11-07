namespace SuracDev.Api.Shared.Infrastructure;

/// <summary>
/// Names of the two OpenAPI documents. A controller declares its group with
/// <c>[ApiExplorerSettings(GroupName = ApiGroups.Public)]</c>; the Angular app generates one client per group.
/// </summary>
public static class ApiGroups
{
  public const string Public = "public";
  public const string Admin = "admin";
}

/// <summary>Named rate-limiting policies registered in <c>Program.cs</c>.</summary>
public static class RateLimitPolicies
{
  public const string Login = "login";
}
