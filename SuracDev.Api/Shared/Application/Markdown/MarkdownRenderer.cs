using System.Text;
using System.Text.RegularExpressions;

namespace SuracDev.Api.Shared.Application.Markdown;

/// <summary>
/// The restricted Markdown renderer for case studies, legal pages and interests
/// (docs/content-model.md): headings, paragraphs, flat lists, fenced code, emphasis, inline code,
/// links and images from uploaded documents. Everything else stays text. All text is HTML-escaped
/// and raw HTML is never passed through, so the output is safe to bind as inner HTML.
/// </summary>
/// <remarks>
/// Written in-house instead of taking a Markdown library: the subset is small, the content comes
/// from one trusted author, and a full CommonMark implementation would add surface without value
/// (docs/decisions.md). Nested lists, tables, block quotes and hard line breaks are not supported.
/// </remarks>
public static partial class MarkdownRenderer
{
  /// <summary>The public URL an image <c>![alt](document:12)</c> resolves to.</summary>
  public const string DocumentUrlPrefix = "/api/public/documents/";

  private const string Fence = "```";

  /// <summary>Renders <paramref name="markdown"/> to HTML; null or blank input renders as an empty string.</summary>
  public static string ToHtml(string? markdown)
  {
    if (string.IsNullOrWhiteSpace(markdown))
    {
      return string.Empty;
    }

    var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    var blocks = new List<string>();
    var index = 0;
    while (index < lines.Length)
    {
      var block = ReadBlock(lines, ref index);
      if (block is not null)
      {
        blocks.Add(block);
      }
    }

    return string.Join('\n', blocks);
  }

  private static string? ReadBlock(string[] lines, ref int index)
  {
    var line = lines[index];
    if (string.IsNullOrWhiteSpace(line))
    {
      index++;
      return null;
    }

    if (line.TrimStart().StartsWith(Fence, StringComparison.Ordinal))
    {
      return ReadCodeBlock(lines, ref index);
    }

    var heading = HeadingPattern().Match(line);
    if (heading.Success)
    {
      index++;
      var level = Math.Max(2, heading.Groups["marks"].Length);
      return $"<h{level}>{Inline(heading.Groups["text"].Value.Trim())}</h{level}>";
    }

    return ListItemText(line, out var ordered) is not null
      ? ReadList(lines, ref index, ordered)
      : ReadParagraph(lines, ref index);
  }

  /// <summary>
  /// A fenced block. The info string carries an optional language and an optional source path
  /// ("csharp Domains/X/Y.cs"); with a path the block becomes a code excerpt captioned with it.
  /// </summary>
  private static string ReadCodeBlock(string[] lines, ref int index)
  {
    var info = InfoPattern().Match(lines[index].Trim()[Fence.Length..].Trim());
    var language = info.Groups["language"].Value;
    var source = info.Groups["source"].Value;
    index++;
    var code = new List<string>();
    while (index < lines.Length && !lines[index].TrimStart().StartsWith(Fence, StringComparison.Ordinal))
    {
      code.Add(lines[index]);
      index++;
    }

    index++; // the closing fence, or past the end when it is missing
    var attribute = language.Length > 0 ? $" class=\"language-{Escape(language)}\"" : string.Empty;
    var block = $"<pre><code{attribute}>{Escape(string.Join('\n', code))}</code></pre>";
    return source.Length == 0
      ? block
      : $"<figure class=\"excerpt\"><figcaption><code>{Escape(source)}</code></figcaption>{block}</figure>";
  }

  private static string ReadList(string[] lines, ref int index, bool ordered)
  {
    var items = new StringBuilder();
    while (index < lines.Length && ListItemText(lines[index], out var itemOrdered) is { } text && itemOrdered == ordered)
    {
      items.Append("<li>").Append(Inline(text)).Append("</li>\n");
      index++;
    }

    var tag = ordered ? "ol" : "ul";
    return $"<{tag}>\n{items}</{tag}>";
  }

  private static string ReadParagraph(string[] lines, ref int index)
  {
    var parts = new List<string>();
    while (index < lines.Length && StartsParagraphLine(lines[index]))
    {
      parts.Add(lines[index].Trim());
      index++;
    }

    return $"<p>{Inline(string.Join(' ', parts))}</p>";
  }

  private static bool StartsParagraphLine(string line)
    => !string.IsNullOrWhiteSpace(line)
      && !line.TrimStart().StartsWith(Fence, StringComparison.Ordinal)
      && !HeadingPattern().IsMatch(line)
      && ListItemText(line, out _) is null;

  private static string? ListItemText(string line, out bool ordered)
  {
    var match = ListItemPattern().Match(line);
    ordered = match.Groups["number"].Success;
    return match.Success ? match.Groups["text"].Value.Trim() : null;
  }

  private static string Inline(string text)
  {
    var html = new StringBuilder();
    var position = 0;
    foreach (Match match in InlinePattern().Matches(text))
    {
      html.Append(Escape(text[position..match.Index]));
      html.Append(InlineElement(match));
      position = match.Index + match.Length;
    }

    return html.Append(Escape(text[position..])).ToString();
  }

  private static string InlineElement(Match match)
  {
    if (match.Groups["code"].Success)
    {
      return $"<code>{Escape(match.Groups["code"].Value)}</code>";
    }

    if (match.Groups["alt"].Success)
    {
      return Image(match.Groups["alt"].Value, match.Groups["src"].Value);
    }

    if (match.Groups["text"].Success)
    {
      return Link(match.Groups["text"].Value, match.Groups["href"].Value);
    }

    return match.Groups["strong"].Success
      ? $"<strong>{Inline(match.Groups["strong"].Value)}</strong>"
      : $"<em>{Inline(match.Groups["em"].Success ? match.Groups["em"].Value : match.Groups["em2"].Value)}</em>";
  }

  private static string Image(string alt, string source)
  {
    var document = DocumentSourcePattern().Match(source);
    return document.Success
      ? $"<img src=\"{DocumentUrlPrefix}{document.Groups["id"].Value}\" alt=\"{Escape(alt)}\" loading=\"lazy\">"
      : Escape(alt);
  }

  private static string Link(string text, string href)
    => IsAllowedHref(href) ? $"<a href=\"{Escape(href)}\">{Inline(text)}</a>" : Inline(text);

  private static bool IsAllowedHref(string href)
    => href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
      || href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
      || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
      || (href.StartsWith('/') && !href.StartsWith("//", StringComparison.Ordinal));

  /// <summary>Escapes the five HTML-significant characters and nothing else, so umlauts stay readable.</summary>
  private static string Escape(string text)
    => text
      .Replace("&", "&amp;", StringComparison.Ordinal)
      .Replace("<", "&lt;", StringComparison.Ordinal)
      .Replace(">", "&gt;", StringComparison.Ordinal)
      .Replace("\"", "&quot;", StringComparison.Ordinal)
      .Replace("'", "&#39;", StringComparison.Ordinal);

  [GeneratedRegex(@"^(?<marks>#{1,4})\s+(?<text>.+)$")]
  private static partial Regex HeadingPattern();

  [GeneratedRegex(@"^\s*(?:[-*]|(?<number>\d+)[.)])\s+(?<text>.*)$")]
  private static partial Regex ListItemPattern();

  [GeneratedRegex(@"^(?<language>[A-Za-z0-9+#-]{1,20})?(?:\s+(?<source>[A-Za-z0-9_./-]{1,200})\s*$)?")]
  private static partial Regex InfoPattern();

  [GeneratedRegex(@"^document:(?<id>\d{1,18})$")]
  private static partial Regex DocumentSourcePattern();

  [GeneratedRegex(
    @"`(?<code>[^`]+)`"
    + @"|!\[(?<alt>[^\]]*)\]\((?<src>(?:[^()\s]|\([^()\s]*\))+)\)"
    + @"|\[(?<text>[^\]]+)\]\((?<href>(?:[^()\s]|\([^()\s]*\))+)\)"
    + @"|\*\*(?<strong>.+?)\*\*"
    + @"|\*(?<em>[^*]+?)\*"
    + @"|(?<!\w)_(?<em2>[^_]+?)_(?!\w)")]
  private static partial Regex InlinePattern();
}
