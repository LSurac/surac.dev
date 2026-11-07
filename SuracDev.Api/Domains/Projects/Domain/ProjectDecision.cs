using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Projects.Domain;

/// <summary>The texts of one decision in one locale.</summary>
public sealed record DecisionTexts(string? Title, string? TradeOff)
{
  /// <summary>True when neither field carries text.</summary>
  public bool IsEmpty => string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(TradeOff);
}

/// <summary>One decision as the admin enters it: German required, English optional.</summary>
public sealed record DecisionInput(DecisionTexts De, DecisionTexts? En);

/// <summary>One architectural choice of a project together with its trade-off.</summary>
public sealed class ProjectDecision
{
  private readonly List<ProjectDecisionTranslation> translations = [];

  private ProjectDecision()
  {
  }

  public long Id { get; private set; }

  public long ProjectId { get; private set; }

  public int SortOrder { get; private set; }

  public IReadOnlyList<ProjectDecisionTranslation> Translations => translations;

  internal static ProjectDecision Create(int sortOrder, DecisionInput input)
  {
    var decision = new ProjectDecision { SortOrder = sortOrder };
    decision.SetTexts(Locale.German, input.De);
    decision.SetTexts(Locale.English, input.En ?? new DecisionTexts(null, null));
    return decision;
  }

  private void SetTexts(Locale locale, DecisionTexts texts)
    => TranslationList.Set(
      translations,
      locale,
      texts.IsEmpty,
      () => ProjectDecisionTranslation.Create(locale, texts),
      translation => translation.Apply(texts));
}

/// <summary>The texts of a decision in one locale.</summary>
public sealed class ProjectDecisionTranslation : ITranslation
{
  private ProjectDecisionTranslation()
  {
  }

  public long Id { get; private set; }

  public long ProjectDecisionId { get; private set; }

  public Locale Locale { get; private set; }

  public string? Title { get; private set; }

  public string? TradeOff { get; private set; }

  internal static ProjectDecisionTranslation Create(Locale locale, DecisionTexts texts)
  {
    var translation = new ProjectDecisionTranslation { Locale = locale };
    translation.Apply(texts);
    return translation;
  }

  internal void Apply(DecisionTexts texts)
  {
    Title = Require.Translated(texts.Title, Locale, "decisions.title", TextLimits.Name, required: true);
    TradeOff = Require.Translated(texts.TradeOff, Locale, "decisions.tradeOff", TextLimits.Paragraph, required: true);
  }
}
