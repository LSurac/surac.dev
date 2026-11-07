using SuracDev.Api.Domains.Career.Domain;
using SuracDev.Api.Domains.Legal.Domain;
using SuracDev.Api.Domains.Profiles.Domain;
using SuracDev.Api.Domains.Projects.Domain;
using SuracDev.Api.Domains.Skills.Domain;
using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Shared.Infrastructure.Seeding;

/// <summary>
/// Neutral example content so a fresh checkout renders every section. Every text says it is an
/// example; the maintainer's real data is entered through the admin and never committed
/// (docs/content-model.md, "Seed").
/// </summary>
internal static class ExampleContent
{
  /// <summary>An example profile in both locales with a full fact bar.</summary>
  public static Profile Profile()
  {
    var profile = Domains.Profiles.Domain.Profile.Create(
      new ProfileContact(
        FullName: "Max Mustermann",
        Email: "max.mustermann@example.org",
        Phone: null,
        Location: "Musterstadt",
        AddressLines: ["Musterstraße 1", "00000 Musterstadt"],
        GitHubUrl: "https://github.com/example",
        LinkedInUrl: null),
      new ProfileTexts(
        TitleLine: "Fullstack-Entwickler · .NET & Angular",
        Headline: "Beispielinhalt – im Admin-Bereich durch echte Daten ersetzen.",
        Intro: "Dieser Text ist ein neutrales Beispiel. Er zeigt, wo das Kurzprofil steht: drei bis fünf Sätze "
          + "darüber, was, womit und in welchem Umfeld entwickelt wird.",
        AvailabilityLine: null));
    profile.SetTexts(
      Locale.English,
      new ProfileTexts(
        TitleLine: "Full-stack developer · .NET & Angular",
        Headline: "Example content – replace it with real data in the admin area.",
        Intro: "This text is a neutral example. It shows where the short profile goes: three to five sentences "
          + "about what is built, with which tools and in which context.",
        AvailabilityLine: null));
    profile.ReplaceFacts(
    [
      new FactInput("5", "Jahre Berufserfahrung (Beispiel)", "Years of experience (example)"),
      new FactInput("1,8", "Abschlussnote (Beispiel)", "Final grade (example)"),
      new FactInput("C1", "Englisch (Beispiel)", "English (example)"),
      new FactInput("2", "Projekte im Betrieb (Beispiel)", "Projects in production (example)"),
    ]);
    return profile;
  }

  /// <summary>One current example career station.</summary>
  public static Experience Experience()
  {
    var experience = Domains.Career.Domain.Experience.Create(
      new ExperienceDetails("Beispiel GmbH", "Musterstadt", new DateOnly(2022, 9, 1), null, 0, IsVisible: true),
      new ExperienceTexts(
        RoleTitle: "Softwareentwickler (Beispiel)",
        Summary: "Beispielhafte Beschreibung einer Berufsstation.",
        Bullets: ["Erster Beispielpunkt", "Zweiter Beispielpunkt"],
        StackLine: ".NET · Angular · PostgreSQL"));
    experience.SetTexts(
      Locale.English,
      new ExperienceTexts(
        RoleTitle: "Software developer (example)",
        Summary: "Example description of a career station.",
        Bullets: ["First example point", "Second example point"],
        StackLine: null));
    return experience;
  }

  /// <summary>One example education entry.</summary>
  public static Education Education()
  {
    var education = Domains.Career.Domain.Education.Create(
      new EducationDetails(
        "Beispiel-Berufsschule", "Musterstadt", new DateOnly(2019, 9, 1), new DateOnly(2022, 6, 30), "1,8", 0, true),
      new EducationTexts("Ausbildung (Beispiel)", null));
    education.SetTexts(Locale.English, new EducationTexts("Apprenticeship (example)", null));
    return education;
  }

  /// <summary>One featured example project with a short case study and one decision.</summary>
  public static Project Project()
  {
    var project = Domains.Projects.Domain.Project.Create(
      new ProjectDetails(
        Slug: "beispielprojekt",
        Status: ProjectStatus.Live,
        StartedYear: 2024,
        EndedYear: null,
        LiveUrl: null,
        RepositoryUrl: null,
        StackTags: [".NET", "Angular", "PostgreSQL"],
        SortOrder: 0,
        IsFeatured: true,
        IsVisible: true),
      new ProjectTexts(
        Title: "Beispielprojekt",
        Teaser: "Ein Beispielprojekt, das zeigt, wie eine Projektkarte aussieht.",
        Problem: "Beispielhafte Problemstellung.",
        MyRole: "Beispielhafte Rolle.",
        Outcome: "Beispielhaftes Ergebnis.",
        CaseStudyMarkdown: "## Ausgangslage\n\nDieser Abschnitt ist ein Beispiel für eine Fallstudie."));
    project.SetTexts(
      Locale.English,
      new ProjectTexts(
        Title: "Example project",
        Teaser: "An example project that shows what a project card looks like.",
        Problem: null,
        MyRole: null,
        Outcome: null,
        CaseStudyMarkdown: "## Starting point\n\nThis section is an example of a case study."));
    project.ReplaceDecisions(
    [
      new DecisionInput(
        new DecisionTexts("Beispielentscheidung", "Beispielhafte Abwägung zwischen zwei Optionen."),
        new DecisionTexts("Example decision", "Example trade-off between two options.")),
    ]);
    return project;
  }

  /// <summary>Two example skill groups with a few skills.</summary>
  public static IEnumerable<SkillGroup> SkillGroups()
  {
    var backend = SkillGroup.Create(0, "Backend", "Backend");
    backend.ReplaceSkills(
    [
      new SkillInput("C#", 2019, null, null),
      new SkillInput("ASP.NET Core", 2020, null, null),
      new SkillInput("PostgreSQL", 2021, null, null),
    ]);

    var frontend = SkillGroup.Create(1, "Frontend", "Frontend");
    frontend.ReplaceSkills(
    [
      new SkillInput("Angular", 2021, null, null),
      new SkillInput("TypeScript", 2021, null, null),
    ]);

    return [backend, frontend];
  }

  /// <summary>Placeholder legal pages; the real texts are written in the admin before launch.</summary>
  public static IEnumerable<LegalPage> LegalPages(DateTimeOffset now)
  {
    var imprint = LegalPage.Create(
      LegalPageKey.Imprint,
      new LegalTexts("Impressum", "Platzhalter: Das Impressum wird im Admin-Bereich gepflegt."),
      now);
    imprint.SetTexts(
      Locale.English,
      new LegalTexts("Legal notice", "Placeholder: the legal notice is maintained in the admin area."),
      now);

    var privacy = LegalPage.Create(
      LegalPageKey.Privacy,
      new LegalTexts("Datenschutzerklärung", "Platzhalter: Die Datenschutzerklärung wird im Admin-Bereich gepflegt."),
      now);
    privacy.SetTexts(
      Locale.English,
      new LegalTexts("Privacy policy", "Placeholder: the privacy policy is maintained in the admin area."),
      now);

    return [imprint, privacy];
  }
}
