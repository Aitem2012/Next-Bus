using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NextBus.Application.Common.Interfaces;
using NextBus.Domain.Entities;

namespace NextBus.Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(AppUser user, IList<string> roles)
        {
            var keyString = _configuration["Jwt:Key"] ?? "NextBus_Super_Secret_Key_For_Authentication_2026_Secure_JWT";
            var issuer = _configuration["Jwt:Issuer"] ?? "NextBusAPI";
            var audience = _configuration["Jwt:Audience"] ?? "NextBusClients";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, $"{user.Firstname} {user.Lastname}".Trim()),
                new("IsKycVerified", user.IsKycVerified.ToString()),
                new("CanOfferRides", user.CanOfferRides.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
