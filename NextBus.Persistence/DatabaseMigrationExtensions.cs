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

                // Ensure Notifications table and RideBooking check-in/out columns
                await EnsureNotificationAndBookingColumnsAsync(context, logger);

                // Seed Roles and Administrator User
                await SeedRolesAndAdminUserAsync(scope.ServiceProvider, configuration, logger);

                // Reconcile any completed rides whose driver earnings were not yet credited
                await ReconcileCompletedRidesPayoutsAsync(context, configuration, logger);

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

        private static async Task EnsureNotificationAndBookingColumnsAsync(AppDbContext context, ILogger logger)
        {
            try
            {
                if (context.Database.IsNpgsql())
                {
                    var sql = @"
ALTER TABLE ""RideBookings"" ADD COLUMN IF NOT EXISTS ""CheckedInAt"" timestamp with time zone;
ALTER TABLE ""RideBookings"" ADD COLUMN IF NOT EXISTS ""CheckedOutAt"" timestamp with time zone;

CREATE TABLE IF NOT EXISTS ""Notifications"" (
    ""Id"" uuid NOT NULL PRIMARY KEY,
    ""AppUserId"" text NOT NULL,
    ""Title"" text NOT NULL,
    ""Message"" text NOT NULL,
    ""Type"" text NOT NULL DEFAULT 'Booking',
    ""IsRead"" boolean NOT NULL DEFAULT false,
    ""ActionUrl"" text,
    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ""IX_Notifications_AppUserId"" ON ""Notifications"" (""AppUserId"");
CREATE INDEX IF NOT EXISTS ""IX_Notifications_IsRead"" ON ""Notifications"" (""IsRead"");
";
                    await context.Database.ExecuteSqlRawAsync(sql);
                }
                else if (context.Database.IsSqlite())
                {
                    var sql = @"
CREATE TABLE IF NOT EXISTS ""Notifications"" (
    ""Id"" TEXT NOT NULL PRIMARY KEY,
    ""AppUserId"" TEXT NOT NULL,
    ""Title"" TEXT NOT NULL,
    ""Message"" TEXT NOT NULL,
    ""Type"" TEXT NOT NULL DEFAULT 'Booking',
    ""IsRead"" INTEGER NOT NULL DEFAULT 0,
    ""ActionUrl"" TEXT,
    ""CreatedAt"" TEXT NOT NULL,
    ""UpdatedAt"" TEXT NOT NULL
);
";
                    await context.Database.ExecuteSqlRawAsync(sql);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Notice while verifying notification/booking tables: {Message}", ex.Message);
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

        private static async Task ReconcileCompletedRidesPayoutsAsync(AppDbContext context, IConfiguration? configuration, ILogger logger)
        {
            try
            {
                var completedRides = await context.Rides
                    .Include(r => r.Bookings)
                    .Where(r => r.Status == RideStatus.Completed && r.Bookings.Any(b => 
                        (b.BookingStatus == BookingStatus.Accepted || b.BookingStatus == BookingStatus.CheckedIn || b.BookingStatus == BookingStatus.CheckedOut) &&
                        b.PaymentStatus == PaymentStatus.Paid))
                    .ToListAsync();

                if (!completedRides.Any()) return;

                var commissionRate = decimal.TryParse(configuration?["PlatformSettings:CommissionPercentage"], out var cp) ? cp : 10.0m;

                foreach (var ride in completedRides)
                {
                    var unpaidBookings = ride.Bookings.Where(b => 
                        (b.BookingStatus == BookingStatus.Accepted || b.BookingStatus == BookingStatus.CheckedIn || b.BookingStatus == BookingStatus.CheckedOut) &&
                        b.PaymentStatus == PaymentStatus.Paid).ToList();

                    if (!unpaidBookings.Any()) continue;

                    decimal totalEarnings = 0m;

                    foreach (var booking in unpaidBookings)
                    {
                        var commission = Math.Round(booking.TotalFare * (commissionRate / 100m), 2);
                        var driverEarnings = booking.TotalFare - commission;

                        booking.CommissionAmount = commission;
                        booking.DriverEarnings = driverEarnings;
                        booking.BookingStatus = BookingStatus.Completed;
                        booking.UpdatedAt = DateTime.UtcNow;

                        totalEarnings += driverEarnings;

                        var commTx = new Transaction
                        {
                            AppUserId = ride.DriverId,
                            Amount = commission,
                            Type = TransactionType.PlatformCommission,
                            Reference = $"COMM_{booking.Id:N}",
                            Description = $"Platform {commissionRate}% commission on passenger booking {booking.Id} for completed ride {ride.Origin} -> {ride.Destination}",
                            IsSuccessful = true,
                            Date = DateTime.UtcNow
                        };
                        await context.Transactions.AddAsync(commTx);
                    }

                    if (totalEarnings > 0)
                    {
                        var driverWallet = await context.Wallets.SingleOrDefaultAsync(w => w.AppUserId == ride.DriverId);
                        if (driverWallet != null)
                        {
                            driverWallet.Balance += totalEarnings;
                            driverWallet.LastUpdated = DateTime.UtcNow;

                            var earnRef = $"EARN_{ride.Id:N}";
                            var history = new WalletHistory
                            {
                                WalletId = driverWallet.Id,
                                AppUserId = ride.DriverId,
                                Amount = totalEarnings,
                                Type = "RideEarnings",
                                Reference = earnRef,
                                Details = $"Reconciled earnings (net of {commissionRate}% fee) for completed ride {ride.Origin} -> {ride.Destination} ({unpaidBookings.Count} passenger(s))",
                                Date = DateTime.UtcNow
                            };
                            await context.WalletHistories.AddAsync(history);

                            var earnTx = new Transaction
                            {
                                AppUserId = ride.DriverId,
                                Amount = totalEarnings,
                                Type = TransactionType.RideEarnings,
                                Reference = earnRef,
                                Description = history.Details,
                                IsSuccessful = true,
                                Date = DateTime.UtcNow
                            };
                            await context.Transactions.AddAsync(earnTx);
                        }
                    }

                    logger.LogInformation("Reconciled completed ride {RideId}: credited ₦{Earnings} to driver {DriverId}", ride.Id, totalEarnings, ride.DriverId);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning("Notice while reconciling completed rides payouts: {Message}", ex.Message);
            }
        }
    }
}
