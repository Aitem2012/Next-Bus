using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;
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

                // Apply additive column additions for Vehicle 4-corner photos and OCR if needed
                await EnsureVehicleColumnsAsync(context, logger);

                // Seed Roles and Administrator User
                await SeedRolesAndAdminUserAsync(scope.ServiceProvider, configuration, logger);

                logger.LogInformation("Database migration and seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database migration: {Message}", ex.Message);
                throw;
            }
        }

        private static async Task EnsureVehicleColumnsAsync(AppDbContext context, ILogger logger)
        {
            try
            {
                if (context.Database.IsNpgsql())
                {
                    var sql = @"
ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""FrontPhotoUrl"" text NOT NULL DEFAULT '';
ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""BackPhotoUrl"" text NOT NULL DEFAULT '';
ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""LeftSidePhotoUrl"" text NOT NULL DEFAULT '';
ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""RightSidePhotoUrl"" text NOT NULL DEFAULT '';
ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""OcrPlateVerified"" boolean NOT NULL DEFAULT false;
ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""OcrExtractedPlateNumber"" text NOT NULL DEFAULT '';
ALTER TABLE ""Vehicles"" ADD COLUMN IF NOT EXISTS ""IsApprovedByAdmin"" boolean NOT NULL DEFAULT true;
";
                    await context.Database.ExecuteSqlRawAsync(sql);
                }
                else if (context.Database.IsSqlite())
                {
                    // In SQLite, columns are created via EnsureCreated / model
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Notice while verifying vehicle columns: {Message}", ex.Message);
            }
        }

        private static async Task SeedRolesAndAdminUserAsync(
            IServiceProvider sp,
            IConfiguration? configuration,
            ILogger logger)
        {
            try
            {
                var roleManager = sp.GetService<RoleManager<IdentityRole>>();
                var userManager = sp.GetService<UserManager<AppUser>>();
                var context = sp.GetRequiredService<AppDbContext>();

                if (roleManager == null || userManager == null)
                {
                    return;
                }

                // 1. Ensure Roles
                var roles = new[] { "Admin", "Driver", "Passenger" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation("Created Identity Role: {Role}", role);
                    }
                }

                // 2. Seed Admin User
                var adminEmail = configuration?["AdminUser:Email"] ?? "admin@nextbus.com";
                var adminPassword = configuration?["AdminUser:Password"] ?? "Admin@NextBus2026!";
                var adminFirstname = configuration?["AdminUser:Firstname"] ?? "System";
                var adminLastname = configuration?["AdminUser:Lastname"] ?? "Administrator";
                var adminPhone = configuration?["AdminUser:PhoneNumber"] ?? "+2348000000000";

                var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
                if (existingAdmin == null)
                {
                    var admin = new AppUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        Firstname = adminFirstname,
                        Lastname = adminLastname,
                        PhoneNumber = adminPhone,
                        EmailConfirmed = true,
                        KycStatus = KycStatus.Verified,
                        DriverKycStatus = DriverKycStatus.Verified,
                        KycVerifiedAt = DateTime.UtcNow,
                        DriverVerifiedAt = DateTime.UtcNow,
                        Wallet = new Wallet
                        {
                            Balance = 50000m,
                            Reference = "NXBS_ADMIN_W",
                            LastUpdated = DateTime.UtcNow
                        }
                    };

                    var createResult = await userManager.CreateAsync(admin, adminPassword);
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                        logger.LogInformation("Admin user {Email} seeded successfully with Admin role.", adminEmail);
                    }
                    else
                    {
                        logger.LogWarning("Failed to seed admin user: {Errors}", string.Join(", ", createResult.Errors));
                    }
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                    {
                        await userManager.AddToRoleAsync(existingAdmin, "Admin");
                        logger.LogInformation("Added existing user {Email} to Admin role.", adminEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Notice while seeding admin user: {Message}", ex.Message);
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
