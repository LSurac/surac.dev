using SuracDev.Api.Domains.Projects.Domain;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Domains.Projects;

public sealed class ProjectTests
{
  private static readonly ProjectDetails Details = new(
    Slug: "beispielprojekt",
    Status: ProjectStatus.Live,
    StartedYear: 2023,
    EndedYear: null,
    LiveUrl: "https://example.org",
    RepositoryUrl: null,
    StackTags: [".NET", " ", "Angular"],
    SortOrder: 0,
    IsFeatured: true,
    IsVisible: true);

  private static readonly ProjectTexts German = new(
    Title: "Beispielprojekt",
    Teaser: "Ein Beispiel.",
    Problem: null,
    MyRole: null,
    Outcome: null,
    CaseStudyMarkdown: "## Problem\n\nText.");

  [Fact]
  public void A_new_project_carries_its_slug_stack_and_german_texts()
  {
    var project = Project.Create(Details, German);

    Assert.Equal("beispielprojekt", project.Slug.Value);
    Assert.Equal([".NET", "Angular"], project.StackTags);
    Assert.Equal("Beispielprojekt", Assert.Single(project.Translations).Title);
  }

  [Fact]
  public void The_title_and_the_teaser_are_required_in_german()
  {
    var exception = Assert.Throws<DomainValidationException>(
      () => Project.Create(Details, German with { Teaser = " " }));

    Assert.Contains("de.teaser", exception.Errors.Keys);
    Assert.Throws<DomainValidationException>(() => Project.Create(Details, German with { Title = null }));
  }

  [Fact]
  public void An_invalid_slug_or_an_unknown_status_is_rejected()
  {
    Assert.Throws<DomainValidationException>(() => Project.Create(Details with { Slug = "Mit Leerzeichen" }, German));
    Assert.Throws<DomainValidationException>(
      () => Project.Create(Details with { Status = ProjectStatus.Unknown }, German));
  }

  [Fact]
  public void The_end_year_may_not_precede_the_start_year()
  {
    var exception = Assert.Throws<DomainValidationException>(
      () => Project.Create(Details with { StartedYear = 2024, EndedYear = 2023 }, German));

    Assert.Contains("endedYear", exception.Errors.Keys);
  }

  [Fact]
  public void Decisions_keep_their_order_and_need_a_german_title_and_trade_off()
  {
    var project = Project.Create(Details, German);

    project.ReplaceDecisions(
    [
      new DecisionInput(new DecisionTexts("Erste", "Abwägung"), new DecisionTexts("First", "Trade-off")),
      new DecisionInput(new DecisionTexts("Zweite", "Abwägung"), null),
    ]);

    Assert.Equal([0, 1], project.Decisions.Select(decision => decision.SortOrder));
    Assert.Equal(2, project.Decisions[0].Translations.Count);
    Assert.Throws<DomainValidationException>(
      () => project.ReplaceDecisions([new DecisionInput(new DecisionTexts("Titel", " "), null)]));
  }

  [Fact]
  public void A_project_has_at_most_five_decisions()
  {
    var project = Project.Create(Details, German);
    var six = Enumerable.Range(1, 6).Select(i => new DecisionInput(new DecisionTexts($"E{i}", "A"), null)).ToList();

    Assert.Throws<DomainValidationException>(() => project.ReplaceDecisions(six));
  }

  [Fact]
  public void Images_reference_documents_in_order_with_optional_captions()
  {
    var project = Project.Create(Details, German);

    project.ReplaceImages([new ImageInput(7, "Startseite", "Start page"), new ImageInput(9, null, null)]);

    Assert.Equal([7L, 9L], project.Images.Select(image => image.DocumentId));
    Assert.Equal(2, project.Images[0].Translations.Count);
    Assert.Single(project.Images[1].Translations);
  }

  [Fact]
  public void Updating_the_details_can_change_the_slug()
  {
    var project = Project.Create(Details, German);

    project.UpdateDetails(Details with { Slug = "neuer-name", IsFeatured = false });

    Assert.Equal("neuer-name", project.Slug.Value);
    Assert.False(project.IsFeatured);
  }

  [Fact]
  public void English_texts_are_optional()
  {
    var project = Project.Create(Details, German);

    project.SetTexts(Locale.English, new ProjectTexts("Example project", null, null, null, null, null));
    Assert.Equal(2, project.Translations.Count);

    project.SetTexts(Locale.English, new ProjectTexts(null, null, null, null, null, " "));
    Assert.Single(project.Translations);
  }
}
