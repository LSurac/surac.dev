using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Projects.Domain;

/// <summary>One image of a case study as the admin enters it: an uploaded document plus captions.</summary>
public sealed record ImageInput(long DocumentId, string? CaptionDe, string? CaptionEn);

/// <summary>A screenshot or diagram of a case study, referencing an uploaded image document.</summary>
public sealed class ProjectImage
{
  private readonly List<ProjectImageTranslation> translations = [];

  private ProjectImage()
  {
  }

  public long Id { get; private set; }

  public long ProjectId { get; private set; }

  public int SortOrder { get; private set; }

  public long DocumentId { get; private set; }

  public IReadOnlyList<ProjectImageTranslation> Translations => translations;

  internal static ProjectImage Create(int sortOrder, ImageInput input)
  {
    var image = new ProjectImage { SortOrder = sortOrder, DocumentId = input.DocumentId };
    image.SetCaption(Locale.German, input.CaptionDe);
    image.SetCaption(Locale.English, input.CaptionEn);
    return image;
  }

  private void SetCaption(Locale locale, string? caption)
    => TranslationList.Set(
      translations,
      locale,
      string.IsNullOrWhiteSpace(caption),
      () => ProjectImageTranslation.Create(locale, caption),
      translation => translation.Apply(caption));
}

/// <summary>The caption of an image in one locale; also its alt text on the public site.</summary>
public sealed class ProjectImageTranslation : ITranslation
{
  private ProjectImageTranslation()
  {
  }

  public long Id { get; private set; }

  public long ProjectImageId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Caption { get; private set; }

  internal static ProjectImageTranslation Create(Locale locale, string? caption)
  {
    var translation = new ProjectImageTranslation { Locale = locale };
    translation.Apply(caption);
    return translation;
  }

  internal void Apply(string? caption)
    => Caption = Require.Translated(caption, Locale, "images.caption", TextLimits.Line);
}
