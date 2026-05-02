using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Services;

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private static readonly Regex GoSplitter = new(@"^\s*GO\s*($|--.*$)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly AccountingDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        AccountingDbContext context,
        IWebHostEnvironment environment,
        ILogger<DatabaseMigrationService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<DatabaseMigrationStatusViewModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await EnsureMetadataTableAsync(cancellationToken);

        var diskScripts = await LoadDiskScriptsAsync(cancellationToken);
        var appliedScripts = await _context.AppliedScripts
            .AsNoTracking()
            .OrderByDescending(x => x.AppliedAtUtc)
            .Select(x => new DatabaseMigrationScriptViewModel
            {
                ScriptName = x.ScriptName,
                AppliedAtUtc = x.AppliedAtUtc,
                ScriptHash = x.ScriptHash
            })
            .ToListAsync(cancellationToken);

        var appliedNameSet = appliedScripts
            .Select(x => x.ScriptName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pendingScripts = diskScripts
            .Where(x => !appliedNameSet.Contains(x.ScriptName))
            .Select(x => new DatabaseMigrationScriptViewModel
            {
                ScriptName = x.ScriptName,
                ScriptHash = x.ScriptHash
            })
            .ToList();

        return new DatabaseMigrationStatusViewModel
        {
            PendingScripts = pendingScripts,
            AppliedScripts = appliedScripts
        };
    }

    public async Task<int> RunPendingAsync(int? appliedByUserId, CancellationToken cancellationToken = default)
    {
        await EnsureMetadataTableAsync(cancellationToken);

        var diskScripts = await LoadDiskScriptsAsync(cancellationToken);
        var appliedNames = await _context.AppliedScripts
            .AsNoTracking()
            .Select(x => x.ScriptName)
            .ToListAsync(cancellationToken);

        var appliedNameSet = appliedNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pendingScripts = diskScripts
            .Where(x => !appliedNameSet.Contains(x.ScriptName))
            .ToList();

        if (pendingScripts.Count == 0)
        {
            return 0;
        }

        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var script in pendingScripts)
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var batch in SplitSqlBatches(script.Content))
                {
                    if (string.IsNullOrWhiteSpace(batch))
                    {
                        continue;
                    }

                    await using var command = connection.CreateCommand();
                    command.Transaction = (SqlTransaction)transaction;
                    command.CommandText = batch;
                    command.CommandTimeout = 120;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = (SqlTransaction)transaction;
                insertCommand.CommandText = """
                    INSERT INTO dbo.AppliedScripts (ScriptName, ScriptHash, AppliedAtUtc, AppliedByUserId)
                    VALUES (@ScriptName, @ScriptHash, SYSUTCDATETIME(), @AppliedByUserId);
                    """;
                insertCommand.Parameters.AddWithValue("@ScriptName", script.ScriptName);
                insertCommand.Parameters.AddWithValue("@ScriptHash", (object?)script.ScriptHash ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@AppliedByUserId", (object?)appliedByUserId ?? DBNull.Value);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed while applying migration script {ScriptName}.", script.ScriptName);
                throw;
            }
        }

        return pendingScripts.Count;
    }

    private async Task EnsureMetadataTableAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.AppliedScripts', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AppliedScripts
                (
                    AppliedScriptId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppliedScripts PRIMARY KEY,
                    ScriptName NVARCHAR(200) NOT NULL,
                    ScriptHash NVARCHAR(64) NULL,
                    AppliedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_AppliedScripts_AppliedAtUtc DEFAULT (SYSUTCDATETIME()),
                    AppliedByUserId INT NULL,
                    CONSTRAINT UX_AppliedScripts_ScriptName UNIQUE (ScriptName),
                    CONSTRAINT FK_AppliedScripts_Users_AppliedByUserId FOREIGN KEY (AppliedByUserId)
                        REFERENCES dbo.Users (UserId)
                );
            END;
            """;

        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task<List<DiskMigrationScript>> LoadDiskScriptsAsync(CancellationToken cancellationToken)
    {
        var candidateFolders = new[]
        {
            Path.Combine(_environment.ContentRootPath, "DatabaseMigrations"),
            Path.Combine(AppContext.BaseDirectory, "DatabaseMigrations")
        }
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        var files = candidateFolders
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.sql", SearchOption.TopDirectoryOnly))
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            _logger.LogWarning(
                "No database migration files were found. Checked folders: {Folders}",
                string.Join(", ", candidateFolders));
            return new List<DiskMigrationScript>();
        }

        var results = new List<DiskMigrationScript>(files.Count);
        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            results.Add(new DiskMigrationScript
            {
                ScriptName = Path.GetFileName(file),
                Content = content,
                ScriptHash = ComputeSha256(content)
            });
        }

        return results;
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        return GoSplitter.Split(script)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    private sealed class DiskMigrationScript
    {
        public string ScriptName { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string ScriptHash { get; init; } = string.Empty;
    }
}
