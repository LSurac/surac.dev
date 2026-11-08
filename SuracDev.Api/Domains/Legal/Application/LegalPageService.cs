using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Legal.Contracts;
using SuracDev.Api.Domains.Legal.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Markdown;

namespace SuracDev.Api.Domains.Legal.Application;

/// <summary>Reads the legal pages for the open site and the admin, and saves the admin's edits.</summary>
public sealed class LegalPageService(SuracDevDbContext dbContext, TimeProvider timeProvider)
{
  /// <summary>
  /// The page in <paramref name="locale"/> with German fallback; the Impressum also carries name,
  /// postal address and e-mail from the profile, so the address is maintained in one place.
  /// </summary>
  /// <exception cref="DomainNotFoundException">The page does not exist.</exception>
  public async Task<PublicLegalPage> GetPublicAsync(
    LegalPageKey key,
    Locale locale,
    CancellationToken cancellationToken)
  {
    var page = await FindAsync(key, tracked: false, cancellationToken);
    var text = new LocalizedText<LegalPageTranslation>(page.Translations, locale);
    var title = text.Text(t => t.Title, nameof(PublicLegalPage.Title))!;
    var body = text.Text(t => t.BodyMarkdown, nameof(PublicLegalPage.BodyHtml));
    var shown = page.Translations.SingleOrDefault(t => t.Locale == locale && !string.IsNullOrWhiteSpace(t.BodyMarkdown))
      ?? page.Translations.Single(t => t.Locale == Locale.German);
    var contact = key == LegalPageKey.Imprint ? await ImprintContactAsync(cancellationToken) : null;

    return new PublicLegalPage(
      page.Key, title, MarkdownRenderer.ToHtml(body), shown.UpdatedAt, contact, text.FallbackFields);
  }

  /// <summary>Both pages with both locales, Impressum first.</summary>
  public async Task<IReadOnlyList<AdminLegalPage>> ListAdminAsync(CancellationToken cancellationToken)
  {
    var pages = await dbContext.LegalPages.AsNoTracking()
      .Include(page => page.Translations)
      .OrderBy(page => page.Key)
      .ToListAsync(cancellationToken);
    return pages.Select(ToAdmin).ToList();
  }

  /// <summary>One page for the editor.</summary>
  /// <exception cref="DomainNotFoundException">The page does not exist.</exception>
  public async Task<AdminLegalPage> GetAdminAsync(LegalPageKey key, CancellationToken cancellationToken)
    => ToAdmin(await FindAsync(key, tracked: false, cancellationToken));

  /// <summary>Writes both locales; only a locale whose text changed gets a new "last updated" stamp.</summary>
  /// <exception cref="DomainNotFoundException">The key names no legal page.</exception>
  /// <exception cref="DomainValidationException">A German title or body is missing.</exception>
  public async Task SaveAsync(LegalPageKey key, LegalPageForm form, CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    var german = new LegalTexts(form.De.Title, form.De.BodyMarkdown);
    var page = await FindOrNullAsync(key, tracked: true, cancellationToken);
    if (page is null)
    {
      page = LegalPage.Create(key, german, now);
      dbContext.LegalPages.Add(page);
    }
    else
    {
      page.SetTexts(Locale.German, german, now);
    }

    page.SetTexts(Locale.English, new LegalTexts(form.En?.Title, form.En?.BodyMarkdown), now);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task<ImprintContact?> ImprintContactAsync(CancellationToken cancellationToken)
    => await dbContext.Profiles.AsNoTracking()
      .OrderBy(profile => profile.Id)
      .Select(profile => new ImprintContact(profile.FullName, profile.AddressLines, profile.Email))
      .FirstOrDefaultAsync(cancellationToken);

  private async Task<LegalPage> FindAsync(LegalPageKey key, bool tracked, CancellationToken cancellationToken)
    => await FindOrNullAsync(key, tracked, cancellationToken)
      ?? throw new DomainNotFoundException($"No legal page '{key}'.");

  private async Task<LegalPage?> FindOrNullAsync(LegalPageKey key, bool tracked, CancellationToken cancellationToken)
  {
    if (key == LegalPageKey.Unknown || !Enum.IsDefined(key))
    {
      throw new DomainNotFoundException($"No legal page '{key}'.");
    }

    var query = dbContext.LegalPages.Include(page => page.Translations);
    return await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(p => p.Key == key, cancellationToken);
  }

  private static AdminLegalPage ToAdmin(LegalPage page)
    => new(
      page.Key,
      ToAdmin(page.Translations.Single(t => t.Locale == Locale.German)),
      page.Translations.Where(t => t.Locale == Locale.English).Select(ToAdmin).SingleOrDefault());

  private static AdminLegalTexts ToAdmin(LegalPageTranslation translation)
    => new(translation.Title!, translation.BodyMarkdown!, translation.UpdatedAt);
}
