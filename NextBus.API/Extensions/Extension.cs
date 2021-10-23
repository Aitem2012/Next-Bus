using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Domain.Users;
using NextBus.Persistence.Context;
using NextBus.Presentation.Users.Commands;
using System;


namespace NextBus.API.Extensions
{
    public static class Extension
    {
        public static IServiceCollection AddServiceCollection(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IAppDbContext, AppDbContext>();
            services.AddIdentity<AppUser, IdentityRole>(options =>
                {
                    // Lockout Settings
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                    options.Lockout.MaxFailedAccessAttempts = 10;

                    // Sign In Settings
                    options.SignIn.RequireConfirmedEmail = true;
                    options.SignIn.RequireConfirmedPhoneNumber = true;

                    // Password Settings
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 6;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredUniqueChars = 0;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;

                    // User Settings
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            services.AddAutoMapper(typeof(Startup));
            services.AddMediatR(typeof(CreateUserCommand).Assembly);
            return services;
        }
    }
}
