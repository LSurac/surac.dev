using System.Text.Json;
using System.Text.Json.Serialization;

namespace SuracDev.Api.Tests.Shared;

/// <summary>The JSON settings of the API (web defaults, enums as strings), for tests that read enums.</summary>
public static class ApiJson
{
  public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };
}
