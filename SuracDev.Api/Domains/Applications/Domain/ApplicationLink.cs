using System.Security.Cryptography;
using System.Text;
using SuracDev.Api.Domains.Documents.Domain;
using SuracDev.Api.Shared.Application.Audience;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Application.Localization;
using SuracDev.Api.Shared.Application.Validation;

namespace SuracDev.Api.Domains.Applications.Domain;

/// <summary>The settings of a link as the admin enters them.</summary>
public sealed record LinkSettings(
  AudienceProfile Profile,
  string? GreetingDe,
  string? GreetingEn,
  DateTimeOffset ExpiresAt,
  bool UnlockFullPdf);

/// <summary>
/// A capability URL <c>/b/&lt;token&gt;</c> for one application. Only the SHA-256 of the token is
/// stored; expired and revoked links answer 404 (docs/security-and-privacy.md, "Application links").
/// </summary>
public sealed class ApplicationLink
{
  /// <summary>The expiry the admin form proposes for a new link.</summary>
  public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(90);

  private readonly List<ApplicationLinkDocument> documents = [];

  private ApplicationLink()
  {
  }

  public long Id { get; private set; }

  public long ApplicationId { get; private set; }

  /// <summary>Lowercase hex SHA-256 of the clear token; the clear token is shown once and never stored.</summary>
  public string TokenHash { get; private set; } = null!;

  public AudienceProfile Profile { get; private set; }

  public string? GreetingDe { get; private set; }

  public string? GreetingEn { get; private set; }

  public DateTimeOffset ExpiresAt { get; private set; }

  public bool IsRevoked { get; private set; }

  /// <summary>Unlocks the full CV PDF with phone and street address.</summary>
  public bool UnlockFullPdf { get; private set; }

  public DateTimeOffset CreatedAt { get; private set; }

  /// <summary>The link-only documents this link unlocks.</summary>
  public IReadOnlyList<ApplicationLinkDocument> Documents => documents;

  /// <summary>Creates a link for an application from its settings and the hash of a fresh token.</summary>
  public static ApplicationLink Create(long applicationId, LinkSettings settings, string tokenHash, DateTimeOffset now)
  {
    var link = new ApplicationLink { ApplicationId = applicationId, TokenHash = tokenHash, CreatedAt = now };
    link.Update(settings, now);
    return link;
  }

  /// <summary>Changes profile, greetings, expiry and the PDF unlock; the expiry must lie in the future.</summary>
  public void Update(LinkSettings settings, DateTimeOffset now)
  {
    if (settings.ExpiresAt <= now)
    {
      throw new DomainValidationException("expiresAt", "Must lie in the future.");
    }

    Profile = Require.Known(settings.Profile, "profile");
    GreetingDe = Require.OptionalText(settings.GreetingDe, "greetingDe", TextLimits.Line);
    GreetingEn = Require.OptionalText(settings.GreetingEn, "greetingEn", TextLimits.Line);
    ExpiresAt = settings.ExpiresAt;
    UnlockFullPdf = settings.UnlockFullPdf;
  }

  /// <summary>Ends the link for good; a revoked link answers 404 like one that never existed.</summary>
  public void Revoke() => IsRevoked = true;

  /// <summary>True when the link may be resolved at <paramref name="now"/>.</summary>
  public bool IsActiveAt(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

  /// <summary>The greeting in <paramref name="locale"/>, falling back to the German one.</summary>
  public string? GreetingFor(Locale locale)
    => locale == Locale.English && !string.IsNullOrWhiteSpace(GreetingEn) ? GreetingEn : GreetingDe;

  /// <summary>Replaces the unlocked documents; only link-only documents can be attached.</summary>
  public void AttachDocuments(IReadOnlyList<Document> attached)
  {
    if (attached.Any(document => document.Visibility != DocumentVisibility.LinkOnly))
    {
      throw new DomainValidationException("documents", "Only link-only documents can be attached to a link.");
    }

    documents.Clear();
    documents.AddRange(attached.Select(document => new ApplicationLinkDocument(Id, document.Id)));
  }
}

/// <summary>Joins a link to a document it unlocks.</summary>
public sealed class ApplicationLinkDocument(long applicationLinkId, long documentId)
{
  public long ApplicationLinkId { get; private set; } = applicationLinkId;

  public long DocumentId { get; private set; } = documentId;
}

/// <summary>Creates and hashes the capability tokens of application links.</summary>
public static class ApplicationLinkToken
{
  private const int TokenBytes = 16;

  /// <summary>A fresh 128-bit token in base64url (22 characters) together with its stored hash.</summary>
  public static (string Token, string Hash) Generate()
  {
    var token = Base64Url(RandomNumberGenerator.GetBytes(TokenBytes));
    return (token, Hash(token));
  }

  /// <summary>Lowercase hex SHA-256 of a clear token, the form in which links are looked up.</summary>
  public static string Hash(string token)
    => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

  private static string Base64Url(byte[] bytes)
    => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
