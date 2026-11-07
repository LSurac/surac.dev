using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuracDev.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "applications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JobAdUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppliedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "education",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "experiences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "legal_pages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedYear = table.Column<int>(type: "integer", nullable: true),
                    EndedYear = table.Column<int>(type: "integer", nullable: true),
                    LiveUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RepositoryUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StackTags = table.Column<string[]>(type: "text[]", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skill_groups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "application_links",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Profile = table.Column<int>(type: "integer", nullable: false),
                    GreetingDe = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GreetingEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    UnlockFullPdf = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_application_links_applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_translations", x => x.Id);
                    table.CheckConstraint("ck_document_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_document_translations_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    ImageDocumentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interests_documents_ImageDocumentId",
                        column: x => x.ImageDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressLines = table.Column<string[]>(type: "text[]", nullable: false),
                    GitHubUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhotoDocumentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_profiles_documents_PhotoDocumentId",
                        column: x => x.PhotoDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "education_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EducationId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Degree = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education_translations", x => x.Id);
                    table.CheckConstraint("ck_education_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_education_translations_education_EducationId",
                        column: x => x.EducationId,
                        principalTable: "education",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExperienceId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    RoleTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    Bullets = table.Column<string[]>(type: "text[]", nullable: false),
                    StackLine = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_translations", x => x.Id);
                    table.CheckConstraint("ck_experience_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_experience_translations_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "legal_page_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LegalPageId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BodyMarkdown = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_page_translations", x => x.Id);
                    table.CheckConstraint("ck_legal_page_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_legal_page_translations_legal_pages_LegalPageId",
                        column: x => x.LegalPageId,
                        principalTable: "legal_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_decisions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_decisions_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_images",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_images_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_images_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Teaser = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Problem = table.Column<string>(type: "text", nullable: true),
                    MyRole = table.Column<string>(type: "text", nullable: true),
                    Outcome = table.Column<string>(type: "text", nullable: true),
                    CaseStudyMarkdown = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_translations", x => x.Id);
                    table.CheckConstraint("ck_project_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_project_translations_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_group_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillGroupId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_group_translations", x => x.Id);
                    table.CheckConstraint("ck_skill_group_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_skill_group_translations_skill_groups_SkillGroupId",
                        column: x => x.SkillGroupId,
                        principalTable: "skill_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillGroupId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SinceYear = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skills_skill_groups_SkillGroupId",
                        column: x => x.SkillGroupId,
                        principalTable: "skill_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "application_link_documents",
                columns: table => new
                {
                    ApplicationLinkId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_link_documents", x => new { x.ApplicationLinkId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_application_link_documents_application_links_ApplicationLin~",
                        column: x => x.ApplicationLinkId,
                        principalTable: "application_links",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_application_link_documents_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "link_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LinkId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DocumentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_link_events", x => x.Id);
                    table.CheckConstraint("ck_link_events_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_link_events_application_links_LinkId",
                        column: x => x.LinkId,
                        principalTable: "application_links",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_link_events_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "interest_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterestId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interest_translations", x => x.Id);
                    table.CheckConstraint("ck_interest_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_interest_translations_interests_InterestId",
                        column: x => x.InterestId,
                        principalTable: "interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_facts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_facts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_profile_facts_profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    TitleLine = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Headline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Intro = table.Column<string>(type: "text", nullable: true),
                    AvailabilityLine = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_translations", x => x.Id);
                    table.CheckConstraint("ck_profile_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_profile_translations_profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_decision_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectDecisionId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TradeOff = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_decision_translations", x => x.Id);
                    table.CheckConstraint("ck_project_decision_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_project_decision_translations_project_decisions_ProjectDeci~",
                        column: x => x.ProjectDecisionId,
                        principalTable: "project_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_image_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectImageId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_image_translations", x => x.Id);
                    table.CheckConstraint("ck_project_image_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_project_image_translations_project_images_ProjectImageId",
                        column: x => x.ProjectImageId,
                        principalTable: "project_images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Qualifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_translations", x => x.Id);
                    table.CheckConstraint("ck_skill_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_skill_translations_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_fact_translations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileFactId = table.Column<long>(type: "bigint", nullable: false),
                    Locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_fact_translations", x => x.Id);
                    table.CheckConstraint("ck_profile_fact_translations_locale", "\"Locale\" IN ('de', 'en')");
                    table.ForeignKey(
                        name: "FK_profile_fact_translations_profile_facts_ProfileFactId",
                        column: x => x.ProfileFactId,
                        principalTable: "profile_facts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_link_documents_DocumentId",
                table: "application_link_documents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_application_links_ApplicationId",
                table: "application_links",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_application_links_TokenHash",
                table: "application_links",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_translations_DocumentId_Locale",
                table: "document_translations",
                columns: new[] { "DocumentId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_StoragePath",
                table: "documents",
                column: "StoragePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_education_translations_EducationId_Locale",
                table: "education_translations",
                columns: new[] { "EducationId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_translations_ExperienceId_Locale",
                table: "experience_translations",
                columns: new[] { "ExperienceId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interest_translations_InterestId_Locale",
                table: "interest_translations",
                columns: new[] { "InterestId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interests_ImageDocumentId",
                table: "interests",
                column: "ImageDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_legal_page_translations_LegalPageId_Locale",
                table: "legal_page_translations",
                columns: new[] { "LegalPageId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legal_pages_Key",
                table: "legal_pages",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_link_events_DocumentId",
                table: "link_events",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_link_events_LinkId",
                table: "link_events",
                column: "LinkId");

            migrationBuilder.CreateIndex(
                name: "IX_profile_fact_translations_ProfileFactId_Locale",
                table: "profile_fact_translations",
                columns: new[] { "ProfileFactId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_facts_ProfileId",
                table: "profile_facts",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_profile_translations_ProfileId_Locale",
                table: "profile_translations",
                columns: new[] { "ProfileId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profiles_PhotoDocumentId",
                table: "profiles",
                column: "PhotoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_project_decision_translations_ProjectDecisionId_Locale",
                table: "project_decision_translations",
                columns: new[] { "ProjectDecisionId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_decisions_ProjectId",
                table: "project_decisions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_image_translations_ProjectImageId_Locale",
                table: "project_image_translations",
                columns: new[] { "ProjectImageId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_images_DocumentId",
                table: "project_images",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_project_images_ProjectId",
                table: "project_images",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_translations_ProjectId_Locale",
                table: "project_translations",
                columns: new[] { "ProjectId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_Slug",
                table: "projects",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skill_group_translations_SkillGroupId_Locale",
                table: "skill_group_translations",
                columns: new[] { "SkillGroupId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skill_translations_SkillId_Locale",
                table: "skill_translations",
                columns: new[] { "SkillId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skills_SkillGroupId",
                table: "skills",
                column: "SkillGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_link_documents");

            migrationBuilder.DropTable(
                name: "document_translations");

            migrationBuilder.DropTable(
                name: "education_translations");

            migrationBuilder.DropTable(
                name: "experience_translations");

            migrationBuilder.DropTable(
                name: "interest_translations");

            migrationBuilder.DropTable(
                name: "legal_page_translations");

            migrationBuilder.DropTable(
                name: "link_events");

            migrationBuilder.DropTable(
                name: "profile_fact_translations");

            migrationBuilder.DropTable(
                name: "profile_translations");

            migrationBuilder.DropTable(
                name: "project_decision_translations");

            migrationBuilder.DropTable(
                name: "project_image_translations");

            migrationBuilder.DropTable(
                name: "project_translations");

            migrationBuilder.DropTable(
                name: "skill_group_translations");

            migrationBuilder.DropTable(
                name: "skill_translations");

            migrationBuilder.DropTable(
                name: "education");

            migrationBuilder.DropTable(
                name: "experiences");

            migrationBuilder.DropTable(
                name: "interests");

            migrationBuilder.DropTable(
                name: "legal_pages");

            migrationBuilder.DropTable(
                name: "application_links");

            migrationBuilder.DropTable(
                name: "profile_facts");

            migrationBuilder.DropTable(
                name: "project_decisions");

            migrationBuilder.DropTable(
                name: "project_images");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "applications");

            migrationBuilder.DropTable(
                name: "profiles");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "skill_groups");

            migrationBuilder.DropTable(
                name: "documents");
        }
    }
}
