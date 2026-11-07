using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;

namespace NextBus.Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private readonly IAppDbContext _context;
        private readonly IEmailService _emailService;

        public OtpService(IAppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<string> GenerateAndSendOtpAsync(string email, string? userId, OtpPurpose purpose, string? metadata = null)
        {
            // Generate a secure 6-digit numeric OTP
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Invalidate any existing unused OTPs for this email and purpose
            var existingOtps = await _context.OtpVerifications
                .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
                .ToListAsync();

            foreach (var existing in existingOtps)
            {
                existing.IsUsed = true;
            }

            var otp = new OtpVerification
            {
                UserId = userId,
                Email = email.Trim().ToLowerInvariant(),
                Code = code,
                Purpose = purpose,
                ExpiryDate = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                Metadata = metadata
            };

            await _context.OtpVerifications.AddAsync(otp);
            await _context.SaveChangesAsync();

            var purposeDisplay = purpose switch
            {
                OtpPurpose.EmailVerification => "Account Email Verification",
                OtpPurpose.PasswordReset => "Password Reset",
                OtpPurpose.Withdrawal => "Wallet Withdrawal Authorization",
                _ => "Verification"
            };

            await _emailService.SendOtpEmailAsync(email, email, code, purposeDisplay);

            return code;
        }

        public async Task<Result<OtpVerification>> VerifyOtpAsync(string email, string code, OtpPurpose purpose)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var trimmedCode = code.Trim();

            var otp = await _context.OtpVerifications
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(o => o.Email == normalizedEmail && o.Purpose == purpose && !o.IsUsed);

            if (otp == null)
            {
                return Result<OtpVerification>.Failure("No active verification code found for this request.");
            }

            if (otp.ExpiryDate < DateTime.UtcNow)
            {
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
                return Result<OtpVerification>.Failure("Verification code has expired. Please request a new one.");
            }

            if (otp.Code != trimmedCode)
            {
                return Result<OtpVerification>.Failure("Invalid verification code.");
            }

            otp.IsUsed = true;
            otp.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<OtpVerification>.Success(otp, "OTP verified successfully.");
        }
    }
}

