namespace IdentityService.Core.Models;

/// <summary>
/// Represents a user record retrieved from the internal Identity/Staff database.
/// </summary>
public sealed record IdentityUserRecord(
    string AggregateId,
    string Username,
    string Name,
    string Email,
    string Phone,
    string? LastPasswordReset = null,
    bool TwoFactorEnabled = false,
    DateTimeOffset? LastLogin = null,
    string? LastLoginIp = null
);
