using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextBus.Application.Common.Interfaces;
using NextBus.Domain.Entities;
using NextBus.Persistence.Context;
using Microsoft.AspNetCore.Identity;

namespace NextBus.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var rawConnectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=NextBus.db";

            services.AddDbContext<AppDbContext>(options =>
            {
                // If PostgreSQL connection string (Host= or Server=)
                if (rawConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                    rawConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseNpgsql(rawConnectionString, b =>
                    {
                        b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    });
                }
                else
                {
                    // SQLite: Anchor relative path to an absolute path so it is never overridden or duplicated across working directories
                    var resolvedConnString = ResolveSqliteConnectionString(rawConnectionString, configuration);

                    options.UseSqlite(resolvedConnString, b =>
                    {
                        b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    });
                }
            });

            services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            return services;
        }

        private static string ResolveSqliteConnectionString(string connString, IConfiguration configuration)
        {
            const string dataSourcePrefix = "Data Source=";
            if (!connString.StartsWith(dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return connString;
            }

            var filePath = connString[dataSourcePrefix.Length..].Trim();

            // Ignore in-memory test strings
            if (filePath.StartsWith(":memory:", StringComparison.OrdinalIgnoreCase) ||
                filePath.Contains("mode=memory", StringComparison.OrdinalIgnoreCase))
            {
                return connString;
            }

            if (!Path.IsPathRooted(filePath))
            {
                // Anchor to ContentRootPath (NextBus.API directory) or AppContext.BaseDirectory
                var baseDirectory = configuration["ContentRootPath"] ?? AppContext.BaseDirectory;

                // If BaseDirectory points inside bin/Debug or bin/Release, walk up so build cleans never delete the db!
                if (baseDirectory.Contains(Path.Combine("bin", "Debug")) || baseDirectory.Contains(Path.Combine("bin", "Release")))
                {
                    var parentDir = Directory.GetParent(baseDirectory)?.Parent?.Parent?.Parent?.FullName;
                    if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(Path.Combine(parentDir, "NextBus.API")))
                    {
                        baseDirectory = Path.Combine(parentDir, "NextBus.API");
                    }
                }

                var fullDbPath = Path.GetFullPath(Path.Combine(baseDirectory, filePath));
                return $"{dataSourcePrefix}{fullDbPath}";
            }

            return connString;
        }
    }
}
