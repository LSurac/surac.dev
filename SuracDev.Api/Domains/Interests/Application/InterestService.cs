using Microsoft.EntityFrameworkCore;
using SuracDev.Api.Data;
using SuracDev.Api.Domains.Documents.Application;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Domains.Interests.Contracts;
using SuracDev.Api.Domains.Interests.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Markdown;

namespace SuracDev.Api.Domains.Interests.Application;

/// <summary>The use cases of the "Über mich" page: the public sections and the admin's CRUD.</summary>
public sealed class InterestService(SuracDevDbContext dbContext, DocumentReferences documentReferences)
{
  /// <summary>Visible interests in page order, in <paramref name="locale"/> with German fallback and rendered Markdown.</summary>
  public async Task<IReadOnlyList<PublicInterest>> ListPublicAsync(Locale locale, CancellationToken cancellationToken)
  {
    var interests = await Ordered(dbContext.Interests.AsNoTracking().Include(interest => interest.Translations))
      .Where(interest => interest.IsVisible)
      .ToListAsync(cancellationToken);
    var imageIds = interests.Select(interest => interest.ImageDocumentId).OfType<long>().ToList();
    var imageLabels = await dbContext.Documents.AsNoTracking()
      .Include(document => document.Translations)
      .Where(document => imageIds.Contains(document.Id))
      .ToDictionaryAsync(
        document => document.Id,
        document => new LocalizedText<DocumentTranslation>(document.Translations, locale).Text(t => t.Label, "label"),
        cancellationToken);

    return interests.Select(interest => ToPublic(interest, locale, imageLabels)).ToList();
  }

  /// <summary>Every interest including hidden ones, in page order.</summary>
  public async Task<IReadOnlyList<AdminInterest>> ListAdminAsync(CancellationToken cancellationToken)
  {
    var interests = await Ordered(dbContext.Interests.AsNoTracking().Include(interest => interest.Translations))
      .ToListAsync(cancellationToken);
    return interests.Select(ToAdmin).ToList();
  }

  /// <summary>One interest for the editor.</summary>
  /// <exception cref="DomainNotFoundException">No interest has this id.</exception>
  public async Task<AdminInterest> GetAdminAsync(long id, CancellationToken cancellationToken)
    => ToAdmin(await FindAsync(id, tracked: false, cancellationToken));

  /// <summary>Creates an interest and returns it as stored.</summary>
  /// <exception cref="DomainValidationException">A field breaks an interest rule or the image is not a public image.</exception>
  public async Task<AdminInterest> CreateAsync(InterestForm form, CancellationToken cancellationToken)
  {
    await EnsureImageAsync(form, cancellationToken);
    var interest = Interest.Create(ToDetails(form), ToTexts(form.De));
    interest.SetTexts(Locale.English, ToTexts(form.En));
    dbContext.Interests.Add(interest);
    await dbContext.SaveChangesAsync(cancellationToken);
    return ToAdmin(interest);
  }

  /// <summary>Replaces an interest with the given fields.</summary>
  /// <exception cref="DomainNotFoundException">No interest has this id.</exception>
  /// <exception cref="DomainValidationException">A field breaks an interest rule or the image is not a public image.</exception>
  public async Task UpdateAsync(long id, InterestForm form, CancellationToken cancellationToken)
  {
    await EnsureImageAsync(form, cancellationToken);
    var interest = await FindAsync(id, tracked: true, cancellationToken);
    interest.UpdateDetails(ToDetails(form));
    interest.SetTexts(Locale.German, ToTexts(form.De));
    interest.SetTexts(Locale.English, ToTexts(form.En));
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>Deletes an interest with its translations; its image stays in the documents.</summary>
  /// <exception cref="DomainNotFoundException">No interest has this id.</exception>
  public async Task DeleteAsync(long id, CancellationToken cancellationToken)
  {
    var interest = await FindAsync(id, tracked: true, cancellationToken);
    dbContext.Interests.Remove(interest);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private Task EnsureImageAsync(InterestForm form, CancellationToken cancellationToken)
  {
    var image = form.ImageDocumentId is { } id ? [id] : Array.Empty<long>();
    return documentReferences.EnsurePublicImagesAsync(image, "imageDocumentId", cancellationToken);
  }

  private static IQueryable<Interest> Ordered(IQueryable<Interest> query)
    => query.OrderBy(interest => interest.SortOrder).ThenBy(interest => interest.Id);

  private async Task<Interest> FindAsync(long id, bool tracked, CancellationToken cancellationToken)
  {
    var query = dbContext.Interests.Include(interest => interest.Translations);
    return await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
      ?? throw new DomainNotFoundException($"Interest {id} does not exist.");
  }

  private static PublicInterest ToPublic(Interest interest, Locale locale, IReadOnlyDictionary<long, string?> imageLabels)
  {
    var text = new LocalizedText<InterestTranslation>(interest.Translations, locale);
    var title = text.Text(t => t.Title, nameof(PublicInterest.Title))!;
    var body = text.Text(t => t.Body, nameof(PublicInterest.BodyHtml));
    var imageAlt = interest.ImageDocumentId is { } imageId ? imageLabels.GetValueOrDefault(imageId) ?? title : null;
    return new PublicInterest(
      interest.Id,
      interest.Kind,
      title,
      body is null ? null : MarkdownRenderer.ToHtml(body),
      interest.ImageDocumentId,
      imageAlt,
      text.FallbackFields);
  }

  private static AdminInterest ToAdmin(Interest interest)
    => new(
      interest.Id,
      interest.Kind,
      interest.SortOrder,
      interest.IsVisible,
      interest.ImageDocumentId,
      ToForm(interest.Translations.Single(t => t.Locale == Locale.German)),
      interest.Translations.Where(t => t.Locale == Locale.English).Select(ToForm).SingleOrDefault());

  private static InterestDetails ToDetails(InterestForm form)
    => new(form.Kind, form.SortOrder, form.IsVisible, form.ImageDocumentId);

  private static InterestTexts ToTexts(InterestTextsForm? form) => new(form?.Title, form?.Body);

  private static InterestTextsForm ToForm(InterestTranslation translation) => new(translation.Title, translation.Body);
}
