using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                if (context.Database.IsSqlite())
                {
                    await context.Database.OpenConnectionAsync();

                    // WAL mode ensures high concurrency and prevents database locks and resets on app restart
                    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                    await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");

                    // Check if existing tables were created without migration history
                    var hasExistingTables = false;
                    using (var command = context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AspNetUsers';";
                        var result = await command.ExecuteScalarAsync();
                        hasExistingTables = Convert.ToInt32(result) > 0;
                    }

                    if (hasExistingTables)
                    {
                        // Baseline the initial migration in __EFMigrationsHistory so MigrateAsync does not attempt duplicate table creation
                        await context.Database.ExecuteSqlRawAsync(@"
                            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                                ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
                                ""ProductVersion"" TEXT NOT NULL
                            );
                            INSERT OR IGNORE INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                            VALUES ('20261107170208_InitialCleanArchitecture', '8.0.10');
                        ");
                    }
                }

                // Safely apply any pending migrations without dropping or resetting tables
                if (context.Database.IsRelational())
                {
                    await context.Database.MigrateAsync();
                }
                else
                {
                    await context.Database.EnsureCreatedAsync();
                }

                logger.LogInformation("Database migration completed successfully. Existing data is preserved across restarts.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database migration: {Message}", ex.Message);
                throw;
            }
        }
    }
}
