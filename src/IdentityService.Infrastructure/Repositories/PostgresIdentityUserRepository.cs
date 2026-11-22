using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using IdentityService.Core.Configuration;
using IdentityService.Core.Interfaces;
using IdentityService.Core.Models;

namespace IdentityService.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of IIdentityUserRepository querying the staff read model.
/// </summary>
public sealed class PostgresIdentityUserRepository : IIdentityUserRepository
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresIdentityUserRepository> _logger;

    public PostgresIdentityUserRepository(
        IOptions<AuthAppSettings> appSettings,
        ILogger<PostgresIdentityUserRepository> logger)
    {
        _connectionString = appSettings.Value.ConnectionStrings.PostgreSQL;
        _logger = logger;
    }

    public async Task<IdentityUserRecord?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var emailLower = normalizedEmail.Trim().ToLowerInvariant();
        _logger.LogInformation("Querying Identity DB for staff member with email: {Email}", emailLower);

        const string sql = @"
            SELECT aggregateid, employeeid, username, name, email, role, dept, phone, 
                   mustresetpassword, lastpasswordreset, twofactorenabled, lastlogin, lastloginip
            FROM staff 
            WHERE LOWER(email) = @Email OR LOWER(username) = @Email 
            LIMIT 1";

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", emailLower);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new IdentityUserRecord(
                    AggregateId: reader.GetString(reader.GetOrdinal("aggregateid")),
                    EmployeeId: reader.GetString(reader.GetOrdinal("employeeid")),
                    Username: reader.GetString(reader.GetOrdinal("username")),
                    Name: reader.GetString(reader.GetOrdinal("name")),
                    Email: reader.GetString(reader.GetOrdinal("email")),
                    Role: reader.GetString(reader.GetOrdinal("role")),
                    Dept: reader.GetString(reader.GetOrdinal("dept")),
                    Phone: reader.GetString(reader.GetOrdinal("phone")),
                    MustResetPassword: reader.GetBoolean(reader.GetOrdinal("mustresetpassword")),
                    LastPasswordReset: reader.IsDBNull(reader.GetOrdinal("lastpasswordreset")) ? null : reader.GetString(reader.GetOrdinal("lastpasswordreset")),
                    TwoFactorEnabled: reader.GetBoolean(reader.GetOrdinal("twofactorenabled")),
                    LastLogin: reader.IsDBNull(reader.GetOrdinal("lastlogin")) ? null : reader.GetDateTime(reader.GetOrdinal("lastlogin")),
                    LastLoginIp: reader.IsDBNull(reader.GetOrdinal("lastloginip")) ? null : reader.GetString(reader.GetOrdinal("lastloginip"))
                );
            }

            _logger.LogWarning("No staff record found in Identity DB for email: {Email}", emailLower);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database query failed while fetching user record for email: {Email}", emailLower);
            throw;
        }
    }

    public async Task RecordLoginAsync(string aggregateId, DateTimeOffset loginTime, string? ipAddress, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE staff 
            SET lastlogin = @LoginTime, lastloginip = @IpAddress 
            WHERE aggregateid = @AggregateId";

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@LoginTime", loginTime);
            command.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);
            command.Parameters.AddWithValue("@AggregateId", aggregateId);

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Updated login metadata for staff aggregate: {AggregateId}", aggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record login timestamp for aggregate: {AggregateId}", aggregateId);
        }
    }

    public async Task UpdatePasswordResetTimestampAsync(string aggregateId, string timestampFormatted, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE staff 
            SET lastpasswordreset = @ResetTime, mustresetpassword = false 
            WHERE aggregateid = @AggregateId";

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ResetTime", timestampFormatted);
            command.Parameters.AddWithValue("@AggregateId", aggregateId);

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Updated password reset timestamp for staff aggregate: {AggregateId}", aggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update password reset timestamp for aggregate: {AggregateId}", aggregateId);
        }
    }
}
