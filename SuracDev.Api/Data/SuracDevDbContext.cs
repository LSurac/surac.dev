using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SuracDev.Api.Domains.Applications.Domain;
using SuracDev.Api.Domains.Career.Domain;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Domains.Identity.Domain;
using SuracDev.Api.Domains.Interests.Domain;
using SuracDev.Api.Domains.Legal.Domain;
using SuracDev.Api.Domains.Profiles.Domain;
using SuracDev.Api.Domains.Projects.Domain;
using SuracDev.Api.Domains.Skills.Domain;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Data;

/// <summary>
/// EF Core context for the surac.dev database. Tables are snake_case, entity properties keep their
/// PascalCase names (quoted columns). All mapping is configured inline here (docs/architecture.md):
/// enums as int, translations in <c>*_translations</c> tables unique per parent and locale, the
/// locale as <c>varchar(2)</c> guarded by a check constraint.
/// </summary>
public sealed class SuracDevDbContext(DbContextOptions<SuracDevDbContext> options) : DbContext(options)
{
  private static readonly ValueConverter<Locale, string> LocaleConverter =
    new(locale => locale.Code, code => Locale.Parse(code));

  private static readonly ValueConverter<Slug, string> SlugConverter =
    new(slug => slug.Value, value => Slug.Parse(value));

  public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

  public DbSet<Profile> Profiles => Set<Profile>();

  public DbSet<Experience> Experiences => Set<Experience>();

  public DbSet<Education> Educations => Set<Education>();

  public DbSet<Project> Projects => Set<Project>();

  public DbSet<SkillGroup> SkillGroups => Set<SkillGroup>();

  public DbSet<LegalPage> LegalPages => Set<LegalPage>();

  public DbSet<Document> Documents => Set<Document>();

  public DbSet<Interest> Interests => Set<Interest>();

  public DbSet<Application> Applications => Set<Application>();

  public DbSet<ApplicationLink> ApplicationLinks => Set<ApplicationLink>();

  public DbSet<LinkEvent> LinkEvents => Set<LinkEvent>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    MapIdentity(modelBuilder);
    MapProfiles(modelBuilder);
    MapCareer(modelBuilder);
    MapProjects(modelBuilder);
    MapSkills(modelBuilder);
    MapLegal(modelBuilder);
    MapDocuments(modelBuilder);
    MapInterests(modelBuilder);
    MapApplications(modelBuilder);
  }

  private static void MapIdentity(ModelBuilder modelBuilder)
    => modelBuilder.Entity<AdminUser>(entity =>
    {
      entity.ToTable("admin_users");
      entity.HasKey(user => user.Id);
      entity.Property(user => user.Id).UseIdentityByDefaultColumn();
      entity.Property(user => user.DisplayName).HasMaxLength(80).IsRequired();
      entity.Property(user => user.PasswordHash).HasMaxLength(500);
      entity.Property(user => user.SecurityStamp).IsRequired();
      entity.Property(user => user.CreatedAt).IsRequired();
    });

  private static void MapProfiles(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Profile>(entity =>
    {
      entity.ToTable("profiles");
      entity.HasKey(profile => profile.Id);
      entity.Property(profile => profile.Id).UseIdentityByDefaultColumn();
      entity.Property(profile => profile.FullName).HasMaxLength(TextLimits.Name).IsRequired();
      entity.Property(profile => profile.Email).HasMaxLength(TextLimits.Name).IsRequired();
      entity.Property(profile => profile.Phone).HasMaxLength(TextLimits.Name);
      entity.Property(profile => profile.Location).HasMaxLength(TextLimits.Name).IsRequired();
      entity.Property(profile => profile.AddressLines).IsRequired();
      entity.Property(profile => profile.GitHubUrl).HasMaxLength(TextLimits.Url);
      entity.Property(profile => profile.LinkedInUrl).HasMaxLength(TextLimits.Url);
      entity.HasOne<Document>().WithMany().HasForeignKey(profile => profile.PhotoDocumentId)
        .OnDelete(DeleteBehavior.Restrict);
      entity.HasMany(profile => profile.Translations).WithOne().HasForeignKey(translation => translation.ProfileId)
        .OnDelete(DeleteBehavior.Cascade);
      entity.HasMany(profile => profile.Facts).WithOne().HasForeignKey(fact => fact.ProfileId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<ProfileTranslation>(
      modelBuilder,
      "profile_translations",
      nameof(ProfileTranslation.ProfileId),
      entity =>
      {
        entity.Property(translation => translation.TitleLine).HasMaxLength(TextLimits.Line);
        entity.Property(translation => translation.Headline).HasMaxLength(TextLimits.Line);
        entity.Property(translation => translation.AvailabilityLine).HasMaxLength(TextLimits.Line);
      });

    modelBuilder.Entity<ProfileFact>(entity =>
    {
      entity.ToTable("profile_facts");
      entity.HasKey(fact => fact.Id);
      entity.Property(fact => fact.Id).UseIdentityByDefaultColumn();
      entity.Property(fact => fact.Value).HasMaxLength(ProfileFact.MaxValueLength).IsRequired();
      entity.HasMany(fact => fact.Translations).WithOne().HasForeignKey(translation => translation.ProfileFactId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<ProfileFactTranslation>(
      modelBuilder,
      "profile_fact_translations",
      nameof(ProfileFactTranslation.ProfileFactId),
      entity => entity.Property(translation => translation.Label).HasMaxLength(TextLimits.Name));
  }

  private static void MapCareer(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Experience>(entity =>
    {
      entity.ToTable("experiences");
      entity.HasKey(experience => experience.Id);
      entity.Property(experience => experience.Id).UseIdentityByDefaultColumn();
      entity.Property(experience => experience.Company).HasMaxLength(TextLimits.Name).IsRequired();
      entity.Property(experience => experience.Location).HasMaxLength(TextLimits.Name);
      entity.Ignore(experience => experience.IsCurrent);
      entity.HasMany(experience => experience.Translations).WithOne()
        .HasForeignKey(translation => translation.ExperienceId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<ExperienceTranslation>(
      modelBuilder,
      "experience_translations",
      nameof(ExperienceTranslation.ExperienceId),
      entity =>
      {
        entity.Property(translation => translation.RoleTitle).HasMaxLength(TextLimits.Name);
        entity.Property(translation => translation.Bullets).IsRequired();
        entity.Property(translation => translation.StackLine).HasMaxLength(TextLimits.Line);
      });

    modelBuilder.Entity<Education>(entity =>
    {
      entity.ToTable("education");
      entity.HasKey(education => education.Id);
      entity.Property(education => education.Id).UseIdentityByDefaultColumn();
      entity.Property(education => education.Institution).HasMaxLength(TextLimits.Name).IsRequired();
      entity.Property(education => education.Location).HasMaxLength(TextLimits.Name);
      entity.Property(education => education.Grade).HasMaxLength(Education.MaxGradeLength);
      entity.HasMany(education => education.Translations).WithOne()
        .HasForeignKey(translation => translation.EducationId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<EducationTranslation>(
      modelBuilder,
      "education_translations",
      nameof(EducationTranslation.EducationId),
      entity => entity.Property(translation => translation.Degree).HasMaxLength(TextLimits.Name));
  }

  private static void MapProjects(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Project>(entity =>
    {
      entity.ToTable("projects");
      entity.HasKey(project => project.Id);
      entity.Property(project => project.Id).UseIdentityByDefaultColumn();
      entity.Property(project => project.Slug).HasConversion(SlugConverter).HasMaxLength(Slug.MaxLength).IsRequired();
      entity.HasIndex(project => project.Slug).IsUnique();
      entity.Property(project => project.Status).HasConversion<int>();
      entity.Property(project => project.LiveUrl).HasMaxLength(TextLimits.Url);
      entity.Property(project => project.RepositoryUrl).HasMaxLength(TextLimits.Url);
      entity.Property(project => project.StackTags).IsRequired();
      entity.HasMany(project => project.Translations).WithOne()
        .HasForeignKey(translation => translation.ProjectId).OnDelete(DeleteBehavior.Cascade);
      entity.HasMany(project => project.Decisions).WithOne()
        .HasForeignKey(decision => decision.ProjectId).OnDelete(DeleteBehavior.Cascade);
      entity.HasMany(project => project.Images).WithOne()
        .HasForeignKey(image => image.ProjectId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<ProjectTranslation>(
      modelBuilder,
      "project_translations",
      nameof(ProjectTranslation.ProjectId),
      entity =>
      {
        entity.Property(translation => translation.Title).HasMaxLength(TextLimits.Name);
        entity.Property(translation => translation.Teaser).HasMaxLength(TextLimits.Line);
      });

    modelBuilder.Entity<ProjectDecision>(entity =>
    {
      entity.ToTable("project_decisions");
      entity.HasKey(decision => decision.Id);
      entity.Property(decision => decision.Id).UseIdentityByDefaultColumn();
      entity.HasMany(decision => decision.Translations).WithOne()
        .HasForeignKey(translation => translation.ProjectDecisionId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<ProjectDecisionTranslation>(
      modelBuilder,
      "project_decision_translations",
      nameof(ProjectDecisionTranslation.ProjectDecisionId),
      entity => entity.Property(translation => translation.Title).HasMaxLength(TextLimits.Name));

    modelBuilder.Entity<ProjectImage>(entity =>
    {
      entity.ToTable("project_images");
      entity.HasKey(image => image.Id);
      entity.Property(image => image.Id).UseIdentityByDefaultColumn();
      entity.HasOne<Document>().WithMany().HasForeignKey(image => image.DocumentId).OnDelete(DeleteBehavior.Restrict);
      entity.HasMany(image => image.Translations).WithOne()
        .HasForeignKey(translation => translation.ProjectImageId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<ProjectImageTranslation>(
      modelBuilder,
      "project_image_translations",
      nameof(ProjectImageTranslation.ProjectImageId),
      entity => entity.Property(translation => translation.Caption).HasMaxLength(TextLimits.Line));
  }

  private static void MapSkills(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<SkillGroup>(entity =>
    {
      entity.ToTable("skill_groups");
      entity.HasKey(group => group.Id);
      entity.Property(group => group.Id).UseIdentityByDefaultColumn();
      entity.HasMany(group => group.Translations).WithOne()
        .HasForeignKey(translation => translation.SkillGroupId).OnDelete(DeleteBehavior.Cascade);
      entity.HasMany(group => group.Skills).WithOne()
        .HasForeignKey(skill => skill.SkillGroupId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<SkillGroupTranslation>(
      modelBuilder,
      "skill_group_translations",
      nameof(SkillGroupTranslation.SkillGroupId),
      entity => entity.Property(translation => translation.Name).HasMaxLength(TextLimits.Name));

    modelBuilder.Entity<Skill>(entity =>
    {
      entity.ToTable("skills");
      entity.HasKey(skill => skill.Id);
      entity.Property(skill => skill.Id).UseIdentityByDefaultColumn();
      entity.Property(skill => skill.Name).HasMaxLength(Skill.MaxNameLength).IsRequired();
      entity.HasMany(skill => skill.Translations).WithOne()
        .HasForeignKey(translation => translation.SkillId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<SkillTranslation>(
      modelBuilder,
      "skill_translations",
      nameof(SkillTranslation.SkillId),
      entity => entity.Property(translation => translation.Qualifier).HasMaxLength(TextLimits.Name));
  }

  private static void MapLegal(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<LegalPage>(entity =>
    {
      entity.ToTable("legal_pages");
      entity.HasKey(page => page.Id);
      entity.Property(page => page.Id).UseIdentityByDefaultColumn();
      entity.Property(page => page.Key).HasConversion<int>();
      entity.HasIndex(page => page.Key).IsUnique();
      entity.HasMany(page => page.Translations).WithOne()
        .HasForeignKey(translation => translation.LegalPageId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<LegalPageTranslation>(
      modelBuilder,
      "legal_page_translations",
      nameof(LegalPageTranslation.LegalPageId),
      entity => entity.Property(translation => translation.Title).HasMaxLength(TextLimits.Name));
  }

  private static void MapDocuments(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Document>(entity =>
    {
      entity.ToTable("documents");
      entity.HasKey(document => document.Id);
      entity.Property(document => document.Id).UseIdentityByDefaultColumn();
      entity.Property(document => document.FileName).HasMaxLength(Document.MaxFileNameLength).IsRequired();
      entity.Property(document => document.ContentType).HasMaxLength(50).IsRequired();
      entity.Property(document => document.StoragePath).HasMaxLength(Document.MaxStoragePathLength).IsRequired();
      entity.HasIndex(document => document.StoragePath).IsUnique();
      entity.Property(document => document.Sha256).HasMaxLength(Document.Sha256HexLength).IsFixedLength().IsRequired();
      entity.Property(document => document.Kind).HasConversion<int>();
      entity.Property(document => document.Visibility).HasConversion<int>();
      entity.Ignore(document => document.IsPublic);
      entity.HasMany(document => document.Translations).WithOne()
        .HasForeignKey(translation => translation.DocumentId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<DocumentTranslation>(
      modelBuilder,
      "document_translations",
      nameof(DocumentTranslation.DocumentId),
      entity => entity.Property(translation => translation.Label).HasMaxLength(TextLimits.Name));
  }

  private static void MapInterests(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Interest>(entity =>
    {
      entity.ToTable("interests");
      entity.HasKey(interest => interest.Id);
      entity.Property(interest => interest.Id).UseIdentityByDefaultColumn();
      entity.Property(interest => interest.Kind).HasConversion<int>();
      entity.HasOne<Document>().WithMany().HasForeignKey(interest => interest.ImageDocumentId)
        .OnDelete(DeleteBehavior.Restrict);
      entity.HasMany(interest => interest.Translations).WithOne()
        .HasForeignKey(translation => translation.InterestId).OnDelete(DeleteBehavior.Cascade);
    });

    MapTranslation<InterestTranslation>(
      modelBuilder,
      "interest_translations",
      nameof(InterestTranslation.InterestId),
      entity => entity.Property(translation => translation.Title).HasMaxLength(TextLimits.Name));
  }

  private static void MapApplications(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Application>(entity =>
    {
      entity.ToTable("applications");
      entity.HasKey(application => application.Id);
      entity.Property(application => application.Id).UseIdentityByDefaultColumn();
      entity.Property(application => application.Company).HasMaxLength(TextLimits.Name).IsRequired();
      entity.Property(application => application.Position).HasMaxLength(TextLimits.Name).IsRequired();
      entity.Property(application => application.JobAdUrl).HasMaxLength(TextLimits.Url);
      entity.Property(application => application.Status).HasConversion<int>();
    });

    modelBuilder.Entity<ApplicationLink>(entity =>
    {
      entity.ToTable("application_links");
      entity.HasKey(link => link.Id);
      entity.Property(link => link.Id).UseIdentityByDefaultColumn();
      entity.HasOne<Application>().WithMany()
        .HasForeignKey(link => link.ApplicationId)
        .OnDelete(DeleteBehavior.Cascade);
      entity.Property(link => link.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
      entity.HasIndex(link => link.TokenHash).IsUnique();
      entity.Property(link => link.Profile).HasConversion<int>();
      entity.Property(link => link.GreetingDe).HasMaxLength(TextLimits.Line);
      entity.Property(link => link.GreetingEn).HasMaxLength(TextLimits.Line);
      entity.HasMany(link => link.Documents).WithOne()
        .HasForeignKey(document => document.ApplicationLinkId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<ApplicationLinkDocument>(entity =>
    {
      entity.ToTable("application_link_documents");
      entity.HasKey(document => new { document.ApplicationLinkId, document.DocumentId });
      entity.HasOne<Document>().WithMany().HasForeignKey(document => document.DocumentId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<LinkEvent>(entity =>
    {
      entity.ToTable("link_events");
      entity.HasKey(linkEvent => linkEvent.Id);
      entity.Property(linkEvent => linkEvent.Id).UseIdentityByDefaultColumn();
      entity.HasOne<ApplicationLink>().WithMany().HasForeignKey(linkEvent => linkEvent.LinkId)
        .OnDelete(DeleteBehavior.Cascade);
      entity.HasIndex(linkEvent => linkEvent.LinkId);
      entity.Property(linkEvent => linkEvent.Kind).HasConversion<int>();
      MapLocale(entity.Property(linkEvent => linkEvent.Locale), entity, "link_events");
      // A deleted document keeps the statistic: the event stays, without its document.
      entity.HasOne<Document>().WithMany().HasForeignKey(linkEvent => linkEvent.DocumentId)
        .OnDelete(DeleteBehavior.SetNull);
    });
  }

  /// <summary>
  /// The common shape of every translation table: identity key, the locale as checked
  /// <c>varchar(2)</c>, and one row per parent and locale.
  /// </summary>
  private static void MapTranslation<TTranslation>(
    ModelBuilder modelBuilder,
    string table,
    string parentKey,
    Action<EntityTypeBuilder<TTranslation>> configure)
    where TTranslation : class, ITranslation
    => modelBuilder.Entity<TTranslation>(entity =>
    {
      entity.ToTable(table);
      entity.HasKey("Id");
      entity.Property<long>("Id").UseIdentityByDefaultColumn();
      MapLocale(entity.Property(translation => translation.Locale), entity, table);
      entity.HasIndex(parentKey, nameof(ITranslation.Locale)).IsUnique();
      configure(entity);
    });

  private static void MapLocale<TEntity>(
    PropertyBuilder<Locale> property,
    EntityTypeBuilder<TEntity> entity,
    string table)
    where TEntity : class
  {
    property.HasConversion(LocaleConverter).HasMaxLength(2).IsRequired();
    entity.ToTable(builder => builder.HasCheckConstraint($"ck_{table}_locale", "\"Locale\" IN ('de', 'en')"));
  }
}
