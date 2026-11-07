using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Domains.Documents.Application;
using SuracDev.Api.Domains.Documents.Contracts;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Domains.Documents.Controllers;

/// <summary>Uploaded documents in the admin area: photo, case-study images, certificates, cover letters.</summary>
[ApiController]
[Route("api/admin/documents")]
[ApiExplorerSettings(GroupName = ApiGroups.Admin)]
public sealed class AdminDocumentsController(DocumentService documentService) : ControllerBase
{
  /// <summary>Room for the multipart envelope and the form fields around a file of the maximum size.</summary>
  private const long UploadRequestLimit = Document.MaxSizeBytes + (64 * 1024);

  /// <summary>Lists every document, newest first.</summary>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">All documents.</response>
  /// <response code="401">Not logged in.</response>
  [HttpGet]
  [Produces("application/json")]
  [ProducesResponseType<IReadOnlyList<AdminDocument>>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public Task<IReadOnlyList<AdminDocument>> List(CancellationToken cancellationToken)
    => documentService.ListAsync(cancellationToken);

  /// <summary>Uploads a document.</summary>
  /// <remarks>
  /// Multipart form with <c>file</c>, <c>kind</c>, <c>visibility</c> and optional <c>labelDe</c> /
  /// <c>labelEn</c>. The content type is detected from the file's first bytes; the name and the type
  /// the browser sends are ignored. Allowed: PDF, PNG, JPEG, WebP up to 10 MB. Photos and images must
  /// be images; certificates and cover letters must be link-only. Images are not resized: upload them
  /// in web size.
  /// </remarks>
  /// <param name="file">The file.</param>
  /// <param name="kind">What the document is.</param>
  /// <param name="visibility">Public (open site) or link-only (application links only).</param>
  /// <param name="labelDe">Optional German label, also the alt text of an image.</param>
  /// <param name="labelEn">Optional English label.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="201">Stored; <c>Location</c> points to the content of the new document.</response>
  /// <response code="400">The file is empty, too large or of a type not allowed, or kind and visibility do not fit (<c>file</c>, <c>visibility</c>).</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="413">The request is larger than a 10 MB file allows.</response>
  [HttpPost]
  [RequestSizeLimit(UploadRequestLimit)]
  [RequestFormLimits(MultipartBodyLengthLimit = UploadRequestLimit)]
  [Consumes("multipart/form-data")]
  [Produces("application/json")]
  [ProducesResponseType<AdminDocument>(StatusCodes.Status201Created)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
  public async Task<ActionResult<AdminDocument>> Upload(
    IFormFile file,
    [FromForm] DocumentKind kind,
    [FromForm] DocumentVisibility visibility,
    [FromForm] string? labelDe,
    [FromForm] string? labelEn,
    CancellationToken cancellationToken)
  {
    var created = await documentService.UploadAsync(file, kind, visibility, labelDe, labelEn, cancellationToken);
    return CreatedAtAction(nameof(Download), new { id = created.Id }, created);
  }

  /// <summary>Changes the labels and the visibility of a document.</summary>
  /// <remarks>
  /// The file itself never changes; upload a new document instead. A document shown on the open site
  /// (profile photo, project or interest image) cannot be made link-only, and certificates and cover
  /// letters can never be made public.
  /// </remarks>
  /// <param name="id">The document's identifier.</param>
  /// <param name="form">The new visibility and labels.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Saved.</response>
  /// <response code="400">The visibility breaks a kind rule.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No document has this id.</response>
  /// <response code="409">The document is shown on the open site and would become link-only.</response>
  [HttpPut("{id:long}")]
  [Produces("application/json")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
  public async Task<IActionResult> Update(long id, DocumentUpdateForm form, CancellationToken cancellationToken)
  {
    await documentService.UpdateAsync(id, form, cancellationToken);
    return NoContent();
  }

  /// <summary>Deletes a document and its file.</summary>
  /// <remarks>A document still used by the profile, a project, an interest or an application link cannot be deleted.</remarks>
  /// <param name="id">The document's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="204">Deleted.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No document has this id.</response>
  /// <response code="409">The document is still in use.</response>
  [HttpDelete("{id:long}")]
  [Produces("application/json")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
  public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
  {
    await documentService.DeleteAsync(id, cancellationToken);
    return NoContent();
  }

  /// <summary>Downloads any document, public or link-only, as an attachment.</summary>
  /// <param name="id">The document's identifier.</param>
  /// <param name="cancellationToken">Aborts the request.</param>
  /// <response code="200">The file with its original name.</response>
  /// <response code="401">Not logged in.</response>
  /// <response code="404">No document has this id.</response>
  [HttpGet("{id:long}/content")]
  [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Download(long id, CancellationToken cancellationToken)
  {
    var document = await documentService.OpenAdminAsync(id, cancellationToken);
    return File(document.Content, document.ContentType, document.FileName);
  }
}
