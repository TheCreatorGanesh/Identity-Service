using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using IdentityService.Core.Configuration;

namespace IdentityService.Infrastructure.Migrations;

/// <summary>
/// Executes embedded SQL migration scripts against PostgreSQL in version order,
/// tracking applied scripts in the <c>schema_migrations</c> table.
/// </summary>
public sealed class PostgresMigrationRunner
{
    private const string MigrationResourcePrefix = "IdentityService.Infrastructure.Migrations.";
    private readonly string _connectionString;
    private readonly ILogger<PostgresMigrationRunner> _logger;

    public PostgresMigrationRunner(
        IOptions<AuthAppSettings> appSettings,
        ILogger<PostgresMigrationRunner> logger)
    {
        _connectionString = appSettings.Value.ConnectionStrings.PostgreSQL;
        _logger = logger;
    }

    /// <summary>
    /// Applies any pending embedded SQL migrations inside individual transactions.
    /// </summary>
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogWarning("PostgreSQL connection string is empty; skipping database migrations.");
            return;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureMigrationTableAsync(connection, cancellationToken);

        var appliedVersions = await GetAppliedVersionsAsync(connection, cancellationToken);
        var assembly = Assembly.GetExecutingAssembly();

        var migrationResources = assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(MigrationResourcePrefix, StringComparison.Ordinal)
                        && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        foreach (var resourceName in migrationResources)
        {
            var version = resourceName[MigrationResourcePrefix.Length..];
            if (appliedVersions.Contains(version))
            {
                continue;
            }

            var sql = await ReadEmbeddedSqlAsync(assembly, resourceName, cancellationToken);
            if (string.IsNullOrWhiteSpace(sql))
            {
                continue;
            }

            _logger.LogInformation("Applying database migration: {Version}", version);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using (var migrateCmd = new NpgsqlCommand(sql, connection, transaction))
                {
                    await migrateCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var recordCmd = new NpgsqlCommand(
                    "INSERT INTO schema_migrations (version) VALUES (@Version);",
                    connection,
                    transaction))
                {
                    recordCmd.Parameters.AddWithValue("@Version", version);
                    await recordCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Successfully applied database migration: {Version}", version);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to apply database migration: {Version}", version);
                throw;
            }
        }
    }

    private static async Task EnsureMigrationTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version VARCHAR(255) NOT NULL PRIMARY KEY,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<HashSet<string>> GetAppliedVersionsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var applied = new HashSet<string>(StringComparer.Ordinal);
        const string sql = "SELECT version FROM schema_migrations;";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(reader.GetString(0));
        }

        return applied;
    }

    private static async Task<string> ReadEmbeddedSqlAsync(
        Assembly assembly,
        string resourceName,
        CancellationToken cancellationToken)
    {
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded migration resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
