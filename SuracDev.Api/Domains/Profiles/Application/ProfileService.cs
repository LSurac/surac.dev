using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Documents.Application;
using SuracDev.Api.Domains.Profiles.Contracts;
using SuracDev.Api.Domains.Profiles.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Domains.Profiles.Application;

/// <summary>Reads the profile for the open site and the admin, and saves the admin's edits.</summary>
public sealed class ProfileService(SuracDevDbContext dbContext, DocumentReferences documentReferences)
{
  /// <summary>The profile in <paramref name="locale"/> with German fallback, without phone and address.</summary>
  /// <exception cref="DomainNotFoundException">No profile row exists (the seeder always creates one).</exception>
  public async Task<PublicProfile> GetPublicAsync(Locale locale, CancellationToken cancellationToken)
  {
    var profile = await LoadAsync(tracked: false, cancellationToken)
      ?? throw new DomainNotFoundException("No profile exists.");

    // Resolved in response order, so fallbackFields lists the fields as the response does.
    var text = new LocalizedText<ProfileTranslation>(profile.Translations, locale);
    var titleLine = text.Text(t => t.TitleLine, nameof(PublicProfile.TitleLine))!;
    var headline = text.Text(t => t.Headline, nameof(PublicProfile.Headline))!;
    var intro = text.Text(t => t.Intro, nameof(PublicProfile.Intro));
    var availabilityLine = text.Text(t => t.AvailabilityLine, nameof(PublicProfile.AvailabilityLine));
    var facts = profile.Facts.Select(fact => ToPublic(fact, locale, text)).ToList();

    return new PublicProfile(
      profile.FullName,
      profile.Email,
      profile.Location,
      profile.GitHubUrl,
      profile.LinkedInUrl,
      profile.PhotoDocumentId,
      titleLine,
      headline,
      intro,
      availabilityLine,
      facts,
      text.FallbackFields);
  }

  /// <summary>The complete profile with both locales for the editor.</summary>
  /// <exception cref="DomainNotFoundException">No profile row exists.</exception>
  public async Task<AdminProfile> GetAdminAsync(CancellationToken cancellationToken)
  {
    var profile = await LoadAsync(tracked: false, cancellationToken)
      ?? throw new DomainNotFoundException("No profile exists.");

    return new AdminProfile(
      profile.FullName,
      profile.Email,
      profile.Phone,
      profile.Location,
      profile.AddressLines,
      profile.GitHubUrl,
      profile.LinkedInUrl,
      profile.PhotoDocumentId,
      ToForm(profile.Translations.Single(t => t.Locale == Locale.German)),
      profile.Translations.Where(t => t.Locale == Locale.English).Select(ToForm).SingleOrDefault(),
      profile.Facts.Select(ToForm).ToList());
  }

  /// <summary>Replaces the whole profile; creates it when the row is missing.</summary>
  /// <exception cref="DomainValidationException">A field breaks a profile rule or the photo is not a public image.</exception>
  public async Task SaveAsync(AdminProfile form, CancellationToken cancellationToken)
  {
    var photo = form.PhotoDocumentId is { } photoId ? [photoId] : Array.Empty<long>();
    await documentReferences.EnsurePublicImagesAsync(photo, "photoDocumentId", cancellationToken);

    var contact = new ProfileContact(
      form.FullName, form.Email, form.Phone, form.Location, form.AddressLines, form.GitHubUrl, form.LinkedInUrl);
    var german = ToTexts(form.De);
    var profile = await LoadAsync(tracked: true, cancellationToken);
    if (profile is null)
    {
      profile = Profile.Create(contact, german);
      dbContext.Profiles.Add(profile);
    }
    else
    {
      profile.UpdateContact(contact);
      profile.SetTexts(Locale.German, german);
    }

    profile.SetTexts(Locale.English, ToTexts(form.En ?? new ProfileTextsForm(null, null, null, null)));
    profile.ReplaceFacts(form.Facts.Select(fact => new FactInput(fact.Value, fact.LabelDe, fact.LabelEn)).ToList());
    profile.SetPhoto(form.PhotoDocumentId);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private Task<Profile?> LoadAsync(bool tracked, CancellationToken cancellationToken)
  {
    var query = dbContext.Profiles
      .Include(profile => profile.Translations)
      .Include(profile => profile.Facts.OrderBy(fact => fact.SortOrder)).ThenInclude(fact => fact.Translations)
      .AsSplitQuery();
    return (tracked ? query : query.AsNoTracking()).OrderBy(profile => profile.Id).FirstOrDefaultAsync(cancellationToken);
  }

  private static PublicFact ToPublic(ProfileFact fact, Locale locale, LocalizedText<ProfileTranslation> parent)
  {
    var label = new LocalizedText<ProfileFactTranslation>(fact.Translations, locale);
    var result = new PublicFact(fact.Value, label.Text(t => t.Label, nameof(PublicFact.Label))!);
    if (label.FallbackFields.Count > 0)
    {
      parent.MarkFallback(nameof(PublicProfile.Facts));
    }

    return result;
  }

  private static ProfileTexts ToTexts(ProfileTextsForm form)
    => new(form.TitleLine, form.Headline, form.Intro, form.AvailabilityLine);

  private static ProfileTextsForm ToForm(ProfileTranslation translation)
    => new(translation.TitleLine, translation.Headline, translation.Intro, translation.AvailabilityLine);

  private static FactForm ToForm(ProfileFact fact)
    => new(
      fact.Value,
      fact.Translations.Single(t => t.Locale == Locale.German).Label!,
      fact.Translations.SingleOrDefault(t => t.Locale == Locale.English)?.Label);
}
