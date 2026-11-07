using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SuracDev.Api.Domains.Pdf.Domain;

namespace SuracDev.Api.Domains.Pdf.Rendering;

/// <summary>
/// Lays out a <see cref="CvModel"/> as an ATS-friendly PDF (docs/decisions.md): one column, real
/// text, standard headings, no icons, no tables. The font is QuestPDF's bundled Lato and system
/// fonts are not consulted, so the output is the same on every machine.
/// </summary>
public static class CvDocument
{
  private const float NameSize = 22;
  private const float TitleSize = 12;
  private const float HeadingSize = 12;
  private const float BodySize = 10;
  private const float MetaSize = 9;
  private static readonly Color Muted = Colors.Grey.Darken2;

  static CvDocument()
  {
    // Free for individuals and small companies; this site is the maintainer's own (questpdf.com/license).
    QuestPDF.Settings.License = LicenseType.Community;
    QuestPDF.Settings.UseSystemFonts = false;
  }

  /// <summary>Renders the CV and returns the PDF bytes.</summary>
  public static byte[] Render(CvModel model)
  {
    var texts = CvTexts.For(model.Locale);
    return Document
      .Create(container => container.Page(page =>
      {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.DefaultTextStyle(style => style.FontSize(BodySize).LineHeight(1.3f));
        page.Content().Column(column =>
        {
          column.Spacing(14);
          column.Item().Element(header => Header(header, model));
          AddSections(column, model, texts);
        });
      }))
      .WithMetadata(new DocumentMetadata
      {
        Title = $"{texts.DocumentPrefix} – {model.FullName}",
        Author = model.FullName,
        Subject = model.TitleLine,
        Language = model.Locale.Code,
      })
      .GeneratePdf();
  }

  private static void AddSections(ColumnDescriptor column, CvModel model, CvTexts texts)
  {
    if (!string.IsNullOrWhiteSpace(model.Intro))
    {
      column.Item().Element(section => Section(section, texts.Profile, body => body.Item().Text(model.Intro)));
    }

    if (model.Experience.Count > 0)
    {
      column.Item().Element(section => Section(section, texts.Experience, body => Entries(body, model.Experience)));
    }

    if (model.Education.Count > 0)
    {
      column.Item().Element(section => Section(section, texts.Education, body => Entries(body, model.Education)));
    }

    if (model.Skills.Count > 0)
    {
      column.Item().Element(section => Section(section, texts.Skills, body => Skills(body, model.Skills)));
    }

    if (model.Projects.Count > 0)
    {
      column.Item().Element(section => Section(section, texts.Projects, body => Projects(body, model.Projects)));
    }
  }

  private static void Header(IContainer container, CvModel model)
    => container.Column(column =>
    {
      column.Item().Text(model.FullName).FontSize(NameSize).SemiBold();
      column.Item().Text(model.TitleLine).FontSize(TitleSize);
      column.Item().PaddingTop(4).Text(string.Join(" · ", model.ContactLines)).FontSize(MetaSize).FontColor(Muted);
    });

  private static void Section(IContainer container, string heading, Action<ColumnDescriptor> content)
    => container.Column(column =>
    {
      column.Spacing(6);
      column.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingBottom(2)
        .Text(heading).FontSize(HeadingSize).SemiBold();
      content(column);
    });

  private static void Entries(ColumnDescriptor column, IReadOnlyList<CvEntry> entries)
  {
    foreach (var entry in entries)
    {
      column.Item().ShowEntire().Column(item =>
      {
        item.Item().Text(text =>
        {
          text.Span(entry.Title).SemiBold();
          text.Span(" – " + entry.Organisation);
        });
        item.Item().Text(entry.Period).FontSize(MetaSize).FontColor(Muted);
        if (!string.IsNullOrWhiteSpace(entry.Summary))
        {
          item.Item().PaddingTop(2).Text(entry.Summary);
        }

        foreach (var bullet in entry.Bullets)
        {
          item.Item().PaddingLeft(8).Text("• " + bullet);
        }

        if (!string.IsNullOrWhiteSpace(entry.Detail))
        {
          item.Item().Text(entry.Detail).FontSize(MetaSize).FontColor(Muted);
        }
      });
    }
  }

  private static void Skills(ColumnDescriptor column, IReadOnlyList<CvSkillGroup> groups)
  {
    foreach (var group in groups)
    {
      column.Item().Text(text =>
      {
        text.Span(group.Name + ": ").SemiBold();
        text.Span(string.Join(", ", group.Skills));
      });
    }
  }

  private static void Projects(ColumnDescriptor column, IReadOnlyList<CvProject> projects)
  {
    foreach (var project in projects)
    {
      column.Item().ShowEntire().Column(item =>
      {
        item.Item().Text(project.Title).SemiBold();
        item.Item().Text(project.Teaser);
        if (!string.IsNullOrWhiteSpace(project.StackLine))
        {
          item.Item().Text(project.StackLine).FontSize(MetaSize).FontColor(Muted);
        }
      });
    }
  }
}
