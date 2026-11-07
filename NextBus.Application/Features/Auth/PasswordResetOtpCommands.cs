using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;

namespace NextBus.Application.Features.Auth
{
    // 1. Request Password Reset OTP
    public record RequestPasswordResetOtpCommand(RequestPasswordResetDto Dto) : IRequest<Result>;

    public class RequestPasswordResetOtpCommandValidator : AbstractValidator<RequestPasswordResetOtpCommand>
    {
        public RequestPasswordResetOtpCommandValidator()
        {
            RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().WithMessage("Valid email is required.");
        }
    }

    public class RequestPasswordResetOtpCommandHandler : IRequestHandler<RequestPasswordResetOtpCommand, Result>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IOtpService _otpService;

        public RequestPasswordResetOtpCommandHandler(
            UserManager<AppUser> userManager,
            IOtpService otpService)
        {
            _userManager = userManager;
            _otpService = otpService;
        }

        public async Task<Result> Handle(RequestPasswordResetOtpCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Dto.Email.Trim());
            if (user != null)
            {
                await _otpService.GenerateAndSendOtpAsync(user.Email!, user.Id, OtpPurpose.PasswordReset);
            }

            // Always return a positive message for security
            return Result.Success("If an account with this email exists, a password reset code has been sent.");
        }
    }

    // 2. Reset Password With OTP
    public record ResetPasswordWithOtpCommand(ResetPasswordWithOtpDto Dto) : IRequest<Result>;

    public class ResetPasswordWithOtpCommandValidator : AbstractValidator<ResetPasswordWithOtpCommand>
    {
        public ResetPasswordWithOtpCommandValidator()
        {
            RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().WithMessage("Valid email is required.");
            RuleFor(x => x.Dto.OtpCode).NotEmpty().Length(6).WithMessage("OTP must be 6 digits.");
            RuleFor(x => x.Dto.NewPassword).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters.");
        }
    }

    public class ResetPasswordWithOtpCommandHandler : IRequestHandler<ResetPasswordWithOtpCommand, Result>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IOtpService _otpService;

        public ResetPasswordWithOtpCommandHandler(
            UserManager<AppUser> userManager,
            IOtpService otpService)
        {
            _userManager = userManager;
            _otpService = otpService;
        }

        public async Task<Result> Handle(ResetPasswordWithOtpCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Dto.Email.Trim());
            if (user == null)
            {
                return Result.Failure("User account not found.");
            }

            // Verify OTP
            var verifyResult = await _otpService.VerifyOtpAsync(
                request.Dto.Email,
                request.Dto.OtpCode,
                OtpPurpose.PasswordReset);

            if (!verifyResult.Succeeded)
            {
                return Result.Failure(verifyResult.Errors);
            }

            // Reset password using Identity token
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, request.Dto.NewPassword);

            if (!result.Succeeded)
            {
                return Result.Failure(result.Errors.Select(e => e.Description));
            }

            return Result.Success("Password reset successfully. You can now log in with your new password.");
        }
    }
}

