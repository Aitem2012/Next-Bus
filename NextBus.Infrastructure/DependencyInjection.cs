using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextBus.Application.Common.Interfaces;
using NextBus.Infrastructure.Services;

namespace NextBus.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();

            // Email & OTP Services
            services.AddTransient<IEmailService, EmailService>();
            services.AddScoped<IOtpService, OtpService>();

            // Identity & Vehicle Verification Services
            services.AddHttpClient<IQoreIdService, QoreIdService>();
            services.AddScoped<IVehicleOcrService, VehicleOcrService>();

            // Paystack Service with typed HttpClient
            services.AddHttpClient<IPaystackService, PaystackService>();

            var keyString = configuration["Jwt:Key"] ?? "NextBus_Super_Secret_Key_For_Authentication_2026_Secure_JWT_Key_Longer_Than_32_Chars";
            var issuer = configuration["Jwt:Issuer"] ?? "NextBusAPI";
            var audience = configuration["Jwt:Audience"] ?? "NextBusClients";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString))
                };
            });

            return services;
        }
    }
}
