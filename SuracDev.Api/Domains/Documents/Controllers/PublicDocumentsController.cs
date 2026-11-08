using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SuracDev.Api.Domains.Documents.Application;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Documents.Controllers;

/// <summary>Public documents of the open site: the profile photo and the case-study images.</summary>
[ApiController]
[AllowAnonymous]
[AnswersHead]
[Route("api/public/documents")]
[ApiExplorerSettings(GroupName = ApiGroups.Public)]
public sealed class PublicDocumentsController(DocumentService documentService) : ControllerBase
{
  /// <summary>How long browsers may reuse a document without asking again.</summary>
  private const string CacheControl = "public, max-age=86400";

  /// <summary>Serves a public document inline.</summary>
  /// <remarks>
  /// Only documents with visibility <c>Public</c> are served; link-only documents answer 404 exactly
  /// like missing ones, so their existence is not confirmed. The response carries the SHA-256 of the
  /// content as ETag; a request with a matching <c>If-None-Match</c> answers 304. Documents never
  /// change their content (a new upload gets a new id), so they may be cached for a day.
  /// </remarks>
  /// <param name="id">The document's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The file, inline, with its detected content type.</response>
  /// <response code="304">The cached copy is current.</response>
  /// <response code="404">No public document has this id.</response>
  [HttpGet("{id:long}")]
  [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status304NotModified)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Get(long id, CancellationToken cancellationToken)
  {
    var document = await documentService.OpenPublicAsync(id, cancellationToken);
    Response.Headers.CacheControl = CacheControl;
    Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
    {
      FileNameStar = document.FileName,
    }.ToString();
    return File(
      document.Content,
      document.ContentType,
      lastModified: document.UploadedAt,
      entityTag: new EntityTagHeaderValue($"\"{document.Sha256}\""));
  }
}
