using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextBus.Persistence.Context;

namespace NextBus.Persistence
{
    public static class DatabaseMigrationExtensions
    {
        public static async Task MigrateDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var configuration = scope.ServiceProvider.GetService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                var connString = configuration?.GetConnectionString("DefaultConnection") 
                    ?? context.Database.GetConnectionString() 
                    ?? "";

                if (context.Database.IsNpgsql())
                {
                    // Ensure the target PostgreSQL database exists before migrating
                    await EnsurePostgreSqlDatabaseExistsAsync(connString, logger);
                }
                else if (context.Database.IsSqlite())
                {
                    await context.Database.OpenConnectionAsync();
                    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                    await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
                }

                // Safely apply EF Core migrations
                if (context.Database.IsRelational())
                {
                    await context.Database.MigrateAsync();
                }
                else
                {
                    await context.Database.EnsureCreatedAsync();
                }

                logger.LogInformation("Database migration completed successfully. Existing data preserved across restarts.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database migration: {Message}", ex.Message);
                throw;
            }
        }

        private static async Task EnsurePostgreSqlDatabaseExistsAsync(string connectionString, ILogger logger)
        {
            try
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                var targetDb = builder.Database;

                if (string.IsNullOrEmpty(targetDb) ||
                    targetDb.Equals("postgres", StringComparison.OrdinalIgnoreCase) ||
                    targetDb.Equals("defaultdb", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Connect to administrative defaultdb to verify and create target database if needed
                var adminBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
                {
                    Database = "defaultdb"
                };

                await using var adminConn = new Npgsql.NpgsqlConnection(adminBuilder.ConnectionString);
                await adminConn.OpenAsync();

                await using var checkCmd = adminConn.CreateCommand();
                checkCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName;";
                checkCmd.Parameters.AddWithValue("dbName", targetDb);
                var exists = await checkCmd.ExecuteScalarAsync() != null;

                if (!exists)
                {
                    logger.LogInformation("Database '{DbName}' does not exist on PostgreSQL server. Creating database...", targetDb);
                    await using var createCmd = adminConn.CreateCommand();
                    createCmd.CommandText = $"CREATE DATABASE \"{targetDb.Replace("\"", "\"\"")}\";";
                    await createCmd.ExecuteNonQueryAsync();
                    logger.LogInformation("Database '{DbName}' created successfully.", targetDb);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Notice while verifying PostgreSQL database existence: {Message}. Proceeding to migrations...", ex.Message);
            }
        }
    }
}
