using SuracDev.Api.Domains.Projects.Contracts;
using SuracDev.Api.Domains.Projects.Domain;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Markdown;

namespace SuracDev.Api.Domains.Projects.Application;

/// <summary>Turns loaded projects into the public and admin contracts, and admin forms into domain input.</summary>
internal static class ProjectMapping
{
  public static PublicProjectCard ToCard(Project project, Locale locale)
  {
    var text = new LocalizedText<ProjectTranslation>(project.Translations, locale);
    var title = text.Text(t => t.Title, nameof(PublicProjectCard.Title))!;
    var teaser = text.Text(t => t.Teaser, nameof(PublicProjectCard.Teaser))!;
    return new PublicProjectCard(
      project.Id,
      project.Slug.Value,
      project.Status,
      project.StartedYear,
      project.EndedYear,
      project.LiveUrl,
      project.RepositoryUrl,
      project.StackTags,
      project.IsFeatured,
      title,
      teaser,
      text.FallbackFields);
  }

  public static PublicCaseStudy ToCaseStudy(Project project, Locale locale)
  {
    var text = new LocalizedText<ProjectTranslation>(project.Translations, locale);
    var title = text.Text(t => t.Title, nameof(PublicCaseStudy.Title))!;
    var teaser = text.Text(t => t.Teaser, nameof(PublicCaseStudy.Teaser))!;
    var problem = text.Text(t => t.Problem, nameof(PublicCaseStudy.Problem));
    var myRole = text.Text(t => t.MyRole, nameof(PublicCaseStudy.MyRole));
    var outcome = text.Text(t => t.Outcome, nameof(PublicCaseStudy.Outcome));
    var caseStudy = text.Text(t => t.CaseStudyMarkdown, nameof(PublicCaseStudy.CaseStudyHtml));
    var decisions = project.Decisions.Select(decision => ToPublic(decision, locale, text)).ToList();
    var images = project.Images.Select(image => ToPublic(image, locale, text)).ToList();
    return new PublicCaseStudy(
      project.Slug.Value,
      project.Status,
      project.StartedYear,
      project.EndedYear,
      project.LiveUrl,
      project.RepositoryUrl,
      project.StackTags,
      title,
      teaser,
      problem,
      myRole,
      outcome,
      MarkdownRenderer.ToHtml(caseStudy),
      decisions,
      images,
      text.FallbackFields);
  }

  public static AdminProject ToAdmin(Project project)
    => new(
      project.Id,
      project.Slug.Value,
      project.Status,
      project.StartedYear,
      project.EndedYear,
      project.LiveUrl,
      project.RepositoryUrl,
      project.StackTags,
      project.SortOrder,
      project.IsFeatured,
      project.IsVisible,
      ToForm(German(project.Translations)),
      English(project.Translations) is { } english ? ToForm(english) : null,
      project.Decisions.Select(ToForm).ToList(),
      project.Images.Select(ToForm).ToList());

  public static ProjectDetails ToDetails(ProjectForm form)
    => new(
      form.Slug,
      form.Status,
      form.StartedYear,
      form.EndedYear,
      form.LiveUrl,
      form.RepositoryUrl,
      form.StackTags,
      form.SortOrder,
      form.IsFeatured,
      form.IsVisible);

  public static ProjectTexts ToTexts(ProjectTextsForm? form)
    => new(form?.Title, form?.Teaser, form?.Problem, form?.MyRole, form?.Outcome, form?.CaseStudyMarkdown);

  public static IReadOnlyList<DecisionInput> ToDecisions(ProjectForm form)
    => form.Decisions
      .Select(decision => new DecisionInput(
        new DecisionTexts(decision.De.Title, decision.De.TradeOff),
        decision.En is null ? null : new DecisionTexts(decision.En.Title, decision.En.TradeOff)))
      .ToList();

  public static IReadOnlyList<ImageInput> ToImages(ProjectForm form)
    => form.Images.Select(image => new ImageInput(image.DocumentId, image.CaptionDe, image.CaptionEn)).ToList();

  private static PublicDecision ToPublic(ProjectDecision decision, Locale locale, LocalizedText<ProjectTranslation> parent)
  {
    var text = new LocalizedText<ProjectDecisionTranslation>(decision.Translations, locale);
    var result = new PublicDecision(
      text.Text(t => t.Title, nameof(PublicDecision.Title))!,
      text.Text(t => t.TradeOff, nameof(PublicDecision.TradeOff))!);
    if (text.FallbackFields.Count > 0)
    {
      parent.MarkFallback(nameof(PublicCaseStudy.Decisions));
    }

    return result;
  }

  private static PublicImage ToPublic(ProjectImage image, Locale locale, LocalizedText<ProjectTranslation> parent)
  {
    var text = new LocalizedText<ProjectImageTranslation>(image.Translations, locale);
    var result = new PublicImage(image.DocumentId, text.Text(t => t.Caption, nameof(PublicImage.Caption)));
    if (text.FallbackFields.Count > 0)
    {
      parent.MarkFallback(nameof(PublicCaseStudy.Images));
    }

    return result;
  }

  private static ProjectTextsForm ToForm(ProjectTranslation translation)
    => new(
      translation.Title,
      translation.Teaser,
      translation.Problem,
      translation.MyRole,
      translation.Outcome,
      translation.CaseStudyMarkdown);

  private static DecisionForm ToForm(ProjectDecision decision)
  {
    var english = English(decision.Translations);
    var german = German(decision.Translations);
    return new DecisionForm(
      new DecisionTextsForm(german.Title, german.TradeOff),
      english is null ? null : new DecisionTextsForm(english.Title, english.TradeOff));
  }

  private static ImageForm ToForm(ProjectImage image)
    => new(image.DocumentId, German(image.Translations).Caption, English(image.Translations)?.Caption);

  private static T German<T>(IEnumerable<T> translations)
    where T : ITranslation
    => translations.Single(translation => translation.Locale == Locale.German);

  private static T? English<T>(IEnumerable<T> translations)
    where T : class, ITranslation
    => translations.SingleOrDefault(translation => translation.Locale == Locale.English);
}
