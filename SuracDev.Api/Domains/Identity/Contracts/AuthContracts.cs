namespace SuracDev.Api.Domains.Identity.Contracts;

public sealed record LoginRequest(string Password);

/// <summary><c>IsInitialLogin</c> tells the UI that this login just set the password.</summary>
public sealed record SessionResponse(long Id, string DisplayName, bool IsInitialLogin);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Whether a password exists yet; the login screen uses it to label the first login.</summary>
public sealed record LoginStateResponse(bool HasPassword);
