using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
    // 1. Verify Email OTP
    public record VerifyEmailOtpCommand(VerifyEmailOtpDto Dto) : IRequest<Result<AuthResponseDto>>;

    public class VerifyEmailOtpCommandValidator : AbstractValidator<VerifyEmailOtpCommand>
    {
        public VerifyEmailOtpCommandValidator()
        {
            RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().WithMessage("Valid email is required.");
            RuleFor(x => x.Dto.OtpCode).NotEmpty().Length(6).WithMessage("OTP must be 6 digits.");
        }
    }

    public class VerifyEmailOtpCommandHandler : IRequestHandler<VerifyEmailOtpCommand, Result<AuthResponseDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IOtpService _otpService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IMapper _mapper;

        public VerifyEmailOtpCommandHandler(
            UserManager<AppUser> userManager,
            IOtpService otpService,
            IJwtTokenService jwtTokenService,
            IMapper mapper)
        {
            _userManager = userManager;
            _otpService = otpService;
            _jwtTokenService = jwtTokenService;
            _mapper = mapper;
        }

        public async Task<Result<AuthResponseDto>> Handle(VerifyEmailOtpCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Dto.Email);
            if (user == null)
            {
                return Result<AuthResponseDto>.Failure("User account not found.");
            }

            if (user.EmailConfirmed)
            {
                var existingRoles = await _userManager.GetRolesAsync(user);
                var existingToken = _jwtTokenService.GenerateToken(user, existingRoles);
                return Result<AuthResponseDto>.Success(new AuthResponseDto
                {
                    Token = existingToken,
                    User = _mapper.Map<UserDto>(user)
                }, "Email is already verified.");
            }

            var verifyResult = await _otpService.VerifyOtpAsync(
                request.Dto.Email,
                request.Dto.OtpCode,
                OtpPurpose.EmailVerification);

            if (!verifyResult.Succeeded)
            {
                return Result<AuthResponseDto>.Failure(verifyResult.Errors);
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);

            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                Token = token,
                User = _mapper.Map<UserDto>(user)
            }, "Email verified successfully.");
        }
    }

    // 2. Resend Email OTP
    public record ResendEmailOtpCommand(string Email) : IRequest<Result>;

    public class ResendEmailOtpCommandHandler : IRequestHandler<ResendEmailOtpCommand, Result>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IOtpService _otpService;

        public ResendEmailOtpCommandHandler(
            UserManager<AppUser> userManager,
            IOtpService otpService)
        {
            _userManager = userManager;
            _otpService = otpService;
        }

        public async Task<Result> Handle(ResendEmailOtpCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result.Failure("Email is required.");
            }

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null)
            {
                // Always succeed generically to prevent user enumeration
                return Result.Success("If an account with this email exists, a new verification code has been sent.");
            }

            if (user.EmailConfirmed)
            {
                return Result.Success("Email is already verified.");
            }

            await _otpService.GenerateAndSendOtpAsync(user.Email!, user.Id, OtpPurpose.EmailVerification);

            return Result.Success("A new 6-digit verification code has been sent to your email.");
        }
    }
}

