using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Projects.Domain;

/// <summary>Where a project stands. Stored as int; <c>Unknown</c> is never written and values are frozen.</summary>
public enum ProjectStatus
{
  Unknown = 0,
  Live = 1,
  InProduction = 2,
  Archived = 3,
  Planned = 4,
}

/// <summary>The structural data of a project as the admin enters it.</summary>
public sealed record ProjectDetails(
  string? Slug,
  ProjectStatus Status,
  int? StartedYear,
  int? EndedYear,
  string? LiveUrl,
  string? RepositoryUrl,
  IReadOnlyList<string?>? StackTags,
  int SortOrder,
  bool IsFeatured,
  bool IsVisible);

/// <summary>The translatable texts of a project card and its case study in one locale.</summary>
public sealed record ProjectTexts(
  string? Title,
  string? Teaser,
  string? Problem,
  string? MyRole,
  string? Outcome,
  string? CaseStudyMarkdown)
{
  /// <summary>True when no field carries text; an empty English set is removed instead of stored.</summary>
  public bool IsEmpty => new[] { Title, Teaser, Problem, MyRole, Outcome, CaseStudyMarkdown }
    .All(string.IsNullOrWhiteSpace);
}

/// <summary>A project card plus its case study: problem, role, decisions with trade-offs, images, outcome.</summary>
public sealed class Project
{
  public const int MaxStackTags = 20;
  public const int MaxDecisions = 5;
  public const int MaxImages = 12;

  private readonly List<ProjectTranslation> translations = [];
  private readonly List<ProjectDecision> decisions = [];
  private readonly List<ProjectImage> images = [];

  private Project()
  {
  }

  public long Id { get; private set; }

  /// <summary>The URL segment shared by both locales; unique across projects.</summary>
  public Slug Slug { get; private set; }

  public ProjectStatus Status { get; private set; }

  public int? StartedYear { get; private set; }

  public int? EndedYear { get; private set; }

  public string? LiveUrl { get; private set; }

  public string? RepositoryUrl { get; private set; }

  /// <summary>Technology names in display order, rendered as a stack line, never as bars.</summary>
  public string[] StackTags { get; private set; } = [];

  public int SortOrder { get; private set; }

  /// <summary>Featured projects appear on the start page; the service caps them.</summary>
  public bool IsFeatured { get; private set; }

  public bool IsVisible { get; private set; }

  public IReadOnlyList<ProjectTranslation> Translations => translations;

  public IReadOnlyList<ProjectDecision> Decisions => decisions;

  public IReadOnlyList<ProjectImage> Images => images;

  /// <summary>Creates a project from its details and the mandatory German texts.</summary>
  public static Project Create(ProjectDetails details, ProjectTexts german)
  {
    var project = new Project();
    project.UpdateDetails(details);
    project.SetTexts(Locale.German, german);
    return project;
  }

  /// <summary>Replaces the structural data after validating it as a whole.</summary>
  public void UpdateDetails(ProjectDetails details)
  {
    var startedYear = Require.Year(details.StartedYear, "startedYear");
    var endedYear = Require.Year(details.EndedYear, "endedYear");
    if (endedYear < startedYear)
    {
      throw new DomainValidationException("endedYear", "Must not be before the start year.");
    }

    Slug = Slug.Parse(details.Slug);
    Status = Require.Known(details.Status, "status");
    StartedYear = startedYear;
    EndedYear = endedYear;
    LiveUrl = Require.OptionalUrl(details.LiveUrl, "liveUrl");
    RepositoryUrl = Require.OptionalUrl(details.RepositoryUrl, "repositoryUrl");
    StackTags = Require.Lines(details.StackTags, "stackTags", TextLimits.Name, MaxStackTags);
    SortOrder = details.SortOrder;
    IsFeatured = details.IsFeatured;
    IsVisible = details.IsVisible;
  }

  /// <summary>Writes the texts of one locale; clearing every English field removes the English set.</summary>
  public void SetTexts(Locale locale, ProjectTexts texts)
    => TranslationList.Set(
      translations,
      locale,
      texts.IsEmpty,
      () => ProjectTranslation.Create(locale, texts),
      translation => translation.Apply(texts));

  /// <summary>Replaces the decisions with the given list, in that order.</summary>
  public void ReplaceDecisions(IReadOnlyList<DecisionInput> input)
  {
    if (input.Count > MaxDecisions)
    {
      throw new DomainValidationException("decisions", $"At most {MaxDecisions} decisions.");
    }

    var replacement = input.Select((decision, index) => ProjectDecision.Create(index, decision)).ToList();
    decisions.Clear();
    decisions.AddRange(replacement);
  }

  /// <summary>Replaces the images with the given list, in that order.</summary>
  public void ReplaceImages(IReadOnlyList<ImageInput> input)
  {
    if (input.Count > MaxImages)
    {
      throw new DomainValidationException("images", $"At most {MaxImages} images.");
    }

    var replacement = input.Select((image, index) => ProjectImage.Create(index, image)).ToList();
    images.Clear();
    images.AddRange(replacement);
  }
}

/// <summary>The texts of a project in one locale.</summary>
public sealed class ProjectTranslation : ITranslation
{
  private ProjectTranslation()
  {
  }

  public long Id { get; private set; }

  public long ProjectId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Title { get; private set; }

  public string? Teaser { get; private set; }

  public string? Problem { get; private set; }

  public string? MyRole { get; private set; }

  public string? Outcome { get; private set; }

  /// <summary>Markdown for the restricted case-study renderer.</summary>
  public string? CaseStudyMarkdown { get; private set; }

  internal static ProjectTranslation Create(Locale locale, ProjectTexts texts)
  {
    var translation = new ProjectTranslation { Locale = locale };
    translation.Apply(texts);
    return translation;
  }

  internal void Apply(ProjectTexts texts)
  {
    Title = Require.Translated(texts.Title, Locale, "title", TextLimits.Name, required: true);
    Teaser = Require.Translated(texts.Teaser, Locale, "teaser", TextLimits.Line, required: true);
    Problem = Require.Translated(texts.Problem, Locale, "problem", TextLimits.Paragraph);
    MyRole = Require.Translated(texts.MyRole, Locale, "myRole", TextLimits.Paragraph);
    Outcome = Require.Translated(texts.Outcome, Locale, "outcome", TextLimits.Paragraph);
    CaseStudyMarkdown = Require.Translated(texts.CaseStudyMarkdown, Locale, "caseStudyMarkdown", TextLimits.Markdown);
  }
}
