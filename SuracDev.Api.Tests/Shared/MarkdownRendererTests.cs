using SuracDev.Api.Shared.Application.Markdown;

namespace SuracDev.Api.Tests.Shared;

public sealed class MarkdownRendererTests
{
  [Fact]
  public void Paragraphs_are_separated_by_blank_lines_and_join_their_lines()
  {
    Assert.Equal("<p>Erste Zeile zweite Zeile</p>\n<p>Neuer Absatz</p>", MarkdownRenderer.ToHtml("Erste Zeile\nzweite Zeile\n\nNeuer Absatz"));
  }

  [Theory]
  [InlineData("# Titel", "<h2>Titel</h2>")]
  [InlineData("## Titel", "<h2>Titel</h2>")]
  [InlineData("### Titel", "<h3>Titel</h3>")]
  [InlineData("#### Titel", "<h4>Titel</h4>")]
  public void Headings_start_at_level_two_because_the_page_owns_the_h1(string markdown, string html)
  {
    Assert.Equal(html, MarkdownRenderer.ToHtml(markdown));
  }

  [Fact]
  public void Unordered_and_ordered_lists_become_lists()
  {
    Assert.Equal(
      "<ul>\n<li>eins</li>\n<li>zwei</li>\n</ul>\n<ol>\n<li>erstens</li>\n<li>zweitens</li>\n</ol>",
      MarkdownRenderer.ToHtml("- eins\n* zwei\n\n1. erstens\n2. zweitens"));
  }

  [Fact]
  public void Fenced_code_keeps_its_lines_and_language_and_is_escaped()
  {
    Assert.Equal(
      "<pre><code class=\"language-csharp\">if (a &lt; b)\n  return;</code></pre>",
      MarkdownRenderer.ToHtml("```csharp\nif (a < b)\n  return;\n```"));
  }

  [Fact]
  public void Inline_emphasis_strong_and_code_are_rendered()
  {
    Assert.Equal(
      "<p><strong>fett</strong>, <em>kursiv</em>, <em>auch</em> und <code>a*b*c</code></p>",
      MarkdownRenderer.ToHtml("**fett**, *kursiv*, _auch_ und `a*b*c`"));
  }

  [Fact]
  public void Underscores_inside_words_stay_text()
  {
    Assert.Equal("<p>snake_case_name</p>", MarkdownRenderer.ToHtml("snake_case_name"));
  }

  [Theory]
  [InlineData("https://example.org/a?b=1&c=2", "https://example.org/a?b=1&amp;c=2")]
  [InlineData("mailto:max@example.org", "mailto:max@example.org")]
  [InlineData("/projekte/beispiel", "/projekte/beispiel")]
  [InlineData("https://de.wikipedia.org/wiki/Test_(Begriff)", "https://de.wikipedia.org/wiki/Test_(Begriff)")]
  public void Links_to_web_mail_and_site_paths_are_kept(string url, string href)
  {
    Assert.Equal($"<p><a href=\"{href}\">Text</a></p>", MarkdownRenderer.ToHtml($"[Text]({url})"));
  }

  [Theory]
  [InlineData("javascript:alert(1)")]
  [InlineData("data:text/html,x")]
  [InlineData("//evil.example")]
  public void Links_with_any_other_target_become_plain_text(string url)
  {
    Assert.Equal("<p>Text</p>", MarkdownRenderer.ToHtml($"[Text]({url})"));
  }

  [Fact]
  public void Images_only_come_from_uploaded_documents()
  {
    Assert.Equal(
      "<p><img src=\"/api/public/documents/12\" alt=\"Startseite\" loading=\"lazy\"></p>",
      MarkdownRenderer.ToHtml("![Startseite](document:12)"));
    Assert.Equal("<p>Fremd</p>", MarkdownRenderer.ToHtml("![Fremd](https://example.org/x.png)"));
  }

  [Fact]
  public void Raw_html_is_escaped_never_rendered()
  {
    Assert.Equal(
      "<p>&lt;script&gt;alert(&quot;x&quot;)&lt;/script&gt; &amp; &lt;b&gt;</p>",
      MarkdownRenderer.ToHtml("<script>alert(\"x\")</script> & <b>"));
  }

  [Fact]
  public void Attribute_values_cannot_break_out_of_their_quotes()
  {
    Assert.Equal(
      "<p><img src=\"/api/public/documents/3\" alt=\"a&quot; onerror=&quot;x\" loading=\"lazy\"></p>",
      MarkdownRenderer.ToHtml("![a\" onerror=\"x](document:3)"));
  }

  [Fact]
  public void Empty_input_renders_nothing()
  {
    Assert.Equal(string.Empty, MarkdownRenderer.ToHtml(null));
    Assert.Equal(string.Empty, MarkdownRenderer.ToHtml("  \n\n "));
  }

  [Fact]
  public void Windows_line_endings_are_handled_like_unix_ones()
  {
    Assert.Equal("<h2>A</h2>\n<p>b</p>", MarkdownRenderer.ToHtml("## A\r\n\r\nb"));
  }

  [Fact]
  public void An_unclosed_code_fence_runs_to_the_end()
  {
    Assert.Equal("<pre><code>code</code></pre>", MarkdownRenderer.ToHtml("```\ncode"));
  }
}
