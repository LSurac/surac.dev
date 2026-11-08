using System.Text.Json;

namespace SuracDev.Api.Shared.Application.Localization;

/// <summary>The locale-specific texts of one entity, stored in its <c>*_translations</c> table.</summary>
public interface ITranslation
{
  Locale Locale { get; }
}

/// <summary>
/// Reads the texts of one entity in a requested locale and falls back to German field by field
/// (docs/content-model.md, "Localisation"). Every field that fell back is recorded under its JSON
/// name, so the public site can decide to hide an untranslated section.
/// </summary>
public sealed class LocalizedText<TTranslation>
  where TTranslation : class, ITranslation
{
  private readonly TTranslation primary;
  private readonly TTranslation? requested;
  private readonly List<string> fallbackFields = [];

  /// <summary>Picks the German and the requested translation; German must exist.</summary>
  /// <exception cref="InvalidOperationException">The entity has no German translation, which the domain never
  /// allows.</exception>
  public LocalizedText(IEnumerable<TTranslation> translations, Locale locale)
  {
    var available = translations.ToList();
    primary = available.SingleOrDefault(translation => translation.Locale == Locale.German)
      ?? throw new InvalidOperationException($"{typeof(TTranslation).Name} has no German translation.");
    requested = locale.IsPrimary
      ? primary
      : available.SingleOrDefault(translation => translation.Locale == locale);
  }

  /// <summary>JSON names of the fields that were read from German instead of the requested locale.</summary>
  public IReadOnlyList<string> FallbackFields => fallbackFields;

  /// <summary>Reads one text field, falling back to German when it is blank in the requested locale.</summary>
  public string? Text(Func<TTranslation, string?> field, string name)
  {
    var value = requested is null ? null : field(requested);
    if (!string.IsNullOrWhiteSpace(value))
    {
      return value;
    }

    var fallback = field(primary);
    if (!string.IsNullOrWhiteSpace(fallback) && requested != primary)
    {
      MarkFallback(name);
    }

    return fallback;
  }

  /// <summary>Reads one list field, falling back to German when it is empty in the requested locale.</summary>
  public IReadOnlyList<string> List(Func<TTranslation, IReadOnlyList<string>> field, string name)
  {
    var value = requested is null ? [] : field(requested);
    if (value.Count > 0)
    {
      return value;
    }

    var fallback = field(primary);
    if (fallback.Count > 0 && requested != primary)
    {
      MarkFallback(name);
    }

    return fallback;
  }

  /// <summary>Records a fallback that happened in a child (a fact label, a decision) under the parent field.</summary>
  public void MarkFallback(string name)
  {
    var jsonName = JsonNamingPolicy.CamelCase.ConvertName(name);
    if (!fallbackFields.Contains(jsonName))
    {
      fallbackFields.Add(jsonName);
    }
  }
}
