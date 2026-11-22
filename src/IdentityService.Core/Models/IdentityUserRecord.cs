namespace IdentityService.Core.Models;

/// <summary>
/// Represents a user record retrieved from the internal Identity/Staff database.
/// </summary>
public sealed record IdentityUserRecord(
    string AggregateId,
    string EmployeeId,
    string Username,
    string Name,
    string Email,
    string Role,
    string Dept,
    string Phone,
    bool MustResetPassword,
    string? LastPasswordReset = null,
    bool TwoFactorEnabled = false,
    DateTimeOffset? LastLogin = null,
    string? LastLoginIp = null
);
