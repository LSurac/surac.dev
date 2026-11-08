using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;

namespace SuracDev.Api.Shared.Infrastructure;

/// <summary>
/// Lets every GET action of a public, read-only controller answer HEAD as well, so link checkers and
/// monitors get the GET status and headers. Without it, routing's 405 for HEAD meets the deny-by-default
/// fallback policy and answers 401. Only the routing metadata changes, so HEAD stays out of the OpenAPI
/// documents and the generated clients. Never put it on a controller whose GET records something, such
/// as the application links (a HEAD would count as an open or a download).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AnswersHeadAttribute : Attribute, IControllerModelConvention
{
  public void Apply(ControllerModel controller)
  {
    foreach (var selector in controller.Actions.SelectMany(action => action.Selectors))
    {
      var get = selector.EndpointMetadata
        .OfType<HttpMethodMetadata>()
        .FirstOrDefault(metadata => metadata.HttpMethods.Contains(HttpMethods.Get));
      if (get is null)
      {
        continue;
      }

      selector.EndpointMetadata.Remove(get);
      selector.EndpointMetadata.Add(
        new HttpMethodMetadata([.. get.HttpMethods, HttpMethods.Head], get.AcceptCorsPreflight));
    }
  }
}
