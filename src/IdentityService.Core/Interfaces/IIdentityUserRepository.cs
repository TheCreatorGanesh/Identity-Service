using IdentityService.Core.Models;

namespace IdentityService.Core.Interfaces;

/// <summary>
/// Access abstraction for querying user and role data from the internal Identity database.
/// </summary>
public interface IIdentityUserRepository
{
    /// <summary>
    /// Finds a user record in the Identity database by normalized (lowercase) email address.
    /// </summary>
    Task<IdentityUserRecord?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records login metadata for audit tracking.
    /// </summary>
    Task RecordLoginAsync(string aggregateId, DateTimeOffset loginTime, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last password reset timestamp for the staff member.
    /// </summary>
    Task UpdatePasswordResetTimestampAsync(string aggregateId, string timestampFormatted, CancellationToken cancellationToken = default);
}
