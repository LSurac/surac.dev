using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Profiles.Domain;

/// <summary>The structural contact data of the profile, written as one unit by the admin.</summary>
public sealed record ProfileContact(
  string? FullName,
  string? Email,
  string? Phone,
  string? Location,
  IReadOnlyList<string?>? AddressLines,
  string? GitHubUrl,
  string? LinkedInUrl);

/// <summary>The translatable texts of the profile in one locale.</summary>
public sealed record ProfileTexts(string? TitleLine, string? Headline, string? Intro, string? AvailabilityLine)
{
  /// <summary>True when no field carries text; an empty English set is removed instead of stored.</summary>
  public bool IsEmpty => string.IsNullOrWhiteSpace(TitleLine) && string.IsNullOrWhiteSpace(Headline)
    && string.IsNullOrWhiteSpace(Intro) && string.IsNullOrWhiteSpace(AvailabilityLine);
}

/// <summary>One cell of the fact bar as the admin enters it.</summary>
public sealed record FactInput(string? Value, string? LabelDe, string? LabelEn);

/// <summary>
/// The person behind the site. Exactly one row. Phone and street address are only for the full PDF
/// and the Impressum (docs/content-model.md, "Visibility rules"); the public read model leaves them out.
/// </summary>
public sealed class Profile
{
  public const int MaxFacts = 4;
  public const int MaxAddressLines = 4;

  private readonly List<ProfileTranslation> translations = [];
  private readonly List<ProfileFact> facts = [];

  private Profile()
  {
  }

  public long Id { get; private set; }

  public string FullName { get; private set; } = null!;

  public string Email { get; private set; } = null!;

  /// <summary>Only in the full PDF; never in the public read model.</summary>
  public string? Phone { get; private set; }

  public string Location { get; private set; } = null!;

  /// <summary>Street address for the full PDF and the Impressum only; never in the public read model.</summary>
  public string[] AddressLines { get; private set; } = [];

  public string? GitHubUrl { get; private set; }

  public string? LinkedInUrl { get; private set; }

  /// <summary>The uploaded photo shown in the hero, or <c>null</c> for none.</summary>
  public long? PhotoDocumentId { get; private set; }

  public IReadOnlyList<ProfileTranslation> Translations => translations;

  public IReadOnlyList<ProfileFact> Facts => facts;

  /// <summary>Creates the profile from its contact data and the mandatory German texts.</summary>
  public static Profile Create(ProfileContact contact, ProfileTexts german)
  {
    var profile = new Profile();
    profile.UpdateContact(contact);
    profile.SetTexts(Locale.German, german);
    return profile;
  }

  /// <summary>Replaces the contact data after validating it as a whole.</summary>
  public void UpdateContact(ProfileContact contact)
  {
    FullName = Require.Text(contact.FullName, "fullName", TextLimits.Name);
    Email = Require.Email(contact.Email, "email");
    Phone = Require.OptionalText(contact.Phone, "phone", TextLimits.Name);
    Location = Require.Text(contact.Location, "location", TextLimits.Name);
    AddressLines = Require.Lines(contact.AddressLines, "addressLines", TextLimits.Name, MaxAddressLines);
    GitHubUrl = Require.OptionalUrl(contact.GitHubUrl, "gitHubUrl");
    LinkedInUrl = Require.OptionalUrl(contact.LinkedInUrl, "linkedInUrl");
  }

  /// <summary>Writes the texts of one locale; clearing every English field removes the English set.</summary>
  public void SetTexts(Locale locale, ProfileTexts texts)
    => TranslationList.Set(
      translations,
      locale,
      texts.IsEmpty,
      () => ProfileTranslation.Create(locale, texts),
      translation => translation.Apply(texts));

  /// <summary>Replaces the fact bar with the given cells, in that order, at most <see cref="MaxFacts"/>.</summary>
  public void ReplaceFacts(IReadOnlyList<FactInput> input)
  {
    if (input.Count > MaxFacts)
    {
      throw new DomainValidationException("facts", $"At most {MaxFacts} facts.");
    }

    var replacement = input.Select((fact, index) => ProfileFact.Create(index, fact)).ToList();
    facts.Clear();
    facts.AddRange(replacement);
  }

  /// <summary>Points the hero photo at an uploaded document, or removes it with <c>null</c>.</summary>
  public void SetPhoto(long? documentId) => PhotoDocumentId = documentId;
}

/// <summary>The texts of the profile in one locale.</summary>
public sealed class ProfileTranslation : ITranslation
{
  private ProfileTranslation()
  {
  }

  public long Id { get; private set; }

  public long ProfileId { get; private set; }

  public Locale Locale { get; private set; }

  public string? TitleLine { get; private set; }

  public string? Headline { get; private set; }

  public string? Intro { get; private set; }

  public string? AvailabilityLine { get; private set; }

  internal static ProfileTranslation Create(Locale locale, ProfileTexts texts)
  {
    var translation = new ProfileTranslation { Locale = locale };
    translation.Apply(texts);
    return translation;
  }

  internal void Apply(ProfileTexts texts)
  {
    TitleLine = Require.Translated(texts.TitleLine, Locale, "titleLine", TextLimits.Line, required: true);
    Headline = Require.Translated(texts.Headline, Locale, "headline", TextLimits.Line, required: true);
    Intro = Require.Translated(texts.Intro, Locale, "intro", TextLimits.Paragraph);
    AvailabilityLine = Require.Translated(texts.AvailabilityLine, Locale, "availabilityLine", TextLimits.Line);
  }
}

/// <summary>One cell of the fact bar: a short value ("5", "1,8", "C1") and a translated label.</summary>
public sealed class ProfileFact
{
  public const int MaxValueLength = 20;

  private readonly List<ProfileFactTranslation> translations = [];

  private ProfileFact()
  {
  }

  public long Id { get; private set; }

  public long ProfileId { get; private set; }

  public int SortOrder { get; private set; }

  public string Value { get; private set; } = null!;

  public IReadOnlyList<ProfileFactTranslation> Translations => translations;

  internal static ProfileFact Create(int sortOrder, FactInput input)
  {
    var fact = new ProfileFact
    {
      SortOrder = sortOrder,
      Value = Require.Text(input.Value, "facts.value", MaxValueLength),
    };
    fact.SetLabel(Locale.German, input.LabelDe);
    fact.SetLabel(Locale.English, input.LabelEn);
    return fact;
  }

  private void SetLabel(Locale locale, string? label)
    => TranslationList.Set(
      translations,
      locale,
      string.IsNullOrWhiteSpace(label),
      () => ProfileFactTranslation.Create(locale, label),
      translation => translation.Apply(label));
}

/// <summary>The label of a fact in one locale.</summary>
public sealed class ProfileFactTranslation : ITranslation
{
  private ProfileFactTranslation()
  {
  }

  public long Id { get; private set; }

  public long ProfileFactId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Label { get; private set; }

  internal static ProfileFactTranslation Create(Locale locale, string? label)
  {
    var translation = new ProfileFactTranslation { Locale = locale };
    translation.Apply(label);
    return translation;
  }

  internal void Apply(string? label)
    => Label = Require.Translated(label, Locale, "facts.label", TextLimits.Name, required: true);
}
