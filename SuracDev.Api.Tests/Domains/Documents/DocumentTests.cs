using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Shared.Application.Exceptions;

namespace SuracDev.Api.Tests.Domains.Documents;

public sealed class DocumentTests
{
  private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
  private static readonly string Sha256 = new('a', 64);

  private static DocumentUpload Upload(
    string fileName = "foto.jpg",
    string contentType = "image/jpeg",
    long sizeBytes = 1024,
    DocumentKind kind = DocumentKind.Photo,
    DocumentVisibility visibility = DocumentVisibility.Public)
    => new(fileName, contentType, sizeBytes, "ab/cdef", Sha256, kind, visibility, "Porträt", null);

  [Fact]
  public void A_new_document_keeps_its_file_facts_and_german_label()
  {
    var document = Document.Create(Upload(), Now);

    Assert.Equal("foto.jpg", document.FileName);
    Assert.Equal(Now, document.UploadedAt);
    Assert.Equal("Porträt", Assert.Single(document.Translations).Label);
  }

  [Fact]
  public void Only_the_file_name_of_a_client_path_is_kept()
  {
    var document = Document.Create(Upload(fileName: @"C:\Users\x\..\foto.jpg"), Now);

    Assert.Equal("foto.jpg", document.FileName);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(Document.MaxSizeBytes + 1)]
  public void Empty_and_oversized_files_are_rejected(long sizeBytes)
  {
    var exception = Assert.Throws<DomainValidationException>(() => Document.Create(Upload(sizeBytes: sizeBytes), Now));

    Assert.Contains("file", exception.Errors.Keys);
  }

  [Fact]
  public void Content_types_outside_pdf_png_jpeg_and_webp_are_rejected()
  {
    Assert.Throws<DomainValidationException>(() => Document.Create(Upload(contentType: "image/svg+xml"), Now));
  }

  [Fact]
  public void Photos_and_images_must_be_images()
  {
    Assert.Throws<DomainValidationException>(
      () => Document.Create(Upload(contentType: "application/pdf", kind: DocumentKind.Image), Now));
  }

  [Theory]
  [InlineData(DocumentKind.Certificate)]
  [InlineData(DocumentKind.CoverLetter)]
  public void Certificates_and_cover_letters_are_never_public(DocumentKind kind)
  {
    var exception = Assert.Throws<DomainValidationException>(
      () => Document.Create(Upload(contentType: "application/pdf", kind: kind), Now));

    Assert.Contains("visibility", exception.Errors.Keys);
    var linkOnly = Document.Create(
      Upload(contentType: "application/pdf", kind: kind, visibility: DocumentVisibility.LinkOnly), Now);
    Assert.Throws<DomainValidationException>(() => linkOnly.ChangeVisibility(DocumentVisibility.Public));
  }

  [Fact]
  public void Unknown_kind_and_visibility_are_rejected()
  {
    Assert.Throws<DomainValidationException>(() => Document.Create(Upload(kind: DocumentKind.Unknown), Now));
    Assert.Throws<DomainValidationException>(
      () => Document.Create(Upload(visibility: DocumentVisibility.Unknown), Now));
  }

  [Fact]
  public void The_label_can_be_changed_in_both_locales()
  {
    var document = Document.Create(Upload(), Now);

    document.Relabel("Foto", "Photo");

    Assert.Equal(2, document.Translations.Count);
  }

  [Fact]
  public void Public_documents_are_served_anonymously_link_only_ones_are_not()
  {
    var photo = Document.Create(Upload(), Now);
    var image = Document.Create(Upload(kind: DocumentKind.Image, visibility: DocumentVisibility.LinkOnly), Now);

    Assert.True(photo.IsPublic);
    Assert.False(image.IsPublic);
  }
}
