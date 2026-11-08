using SuracDev.Api.Shared.Application.Localization;

namespace SuracDev.Api.Tests.Shared;

public sealed class LocalizedTextTests
{
  private sealed record Texts(
    Locale Locale,
    string? Title,
    string? Summary,
    IReadOnlyList<string> Bullets) : ITranslation;

  private static readonly Texts German = new(Locale.German, "Titel", "Zusammenfassung", ["Eins", "Zwei"]);

  [Fact]
  public void German_reads_the_german_texts_without_fallback_marks()
  {
    var text = new LocalizedText<Texts>([German], Locale.German);

    Assert.Equal("Titel", text.Text(t => t.Title, "Title"));
    Assert.Equal(["Eins", "Zwei"], text.List(t => t.Bullets, "Bullets"));
    Assert.Empty(text.FallbackFields);
  }

  [Fact]
  public void English_falls_back_to_german_field_by_field_and_marks_those_fields()
  {
    var english = new Texts(Locale.English, "Title", null, []);
    var text = new LocalizedText<Texts>([German, english], Locale.English);

    Assert.Equal("Title", text.Text(t => t.Title, "Title"));
    Assert.Equal("Zusammenfassung", text.Text(t => t.Summary, "Summary"));
    Assert.Equal(["Eins", "Zwei"], text.List(t => t.Bullets, "Bullets"));
    Assert.Equal(["summary", "bullets"], text.FallbackFields);
  }

  [Fact]
  public void A_missing_english_translation_falls_back_entirely()
  {
    var text = new LocalizedText<Texts>([German], Locale.English);

    Assert.Equal("Titel", text.Text(t => t.Title, "Title"));
    Assert.Equal(["title"], text.FallbackFields);
  }

  [Fact]
  public void A_field_that_is_empty_in_both_locales_is_not_marked_as_fallback()
  {
    var german = German with { Summary = null };
    var text = new LocalizedText<Texts>([german, new Texts(Locale.English, "Title", null, [])], Locale.English);

    Assert.Null(text.Text(t => t.Summary, "Summary"));
    Assert.Empty(text.FallbackFields);
  }

  [Fact]
  public void A_child_fallback_is_recorded_under_the_parent_field()
  {
    var text = new LocalizedText<Texts>([German], Locale.English);

    text.MarkFallback("Facts");
    text.MarkFallback("Facts");

    Assert.Equal(["facts"], text.FallbackFields);
  }

  [Fact]
  public void An_entity_without_german_texts_is_a_programming_error()
  {
    Assert.Throws<InvalidOperationException>(
      () => new LocalizedText<Texts>([new Texts(Locale.English, "Title", null, [])], Locale.English));
  }
}
