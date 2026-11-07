using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;

namespace NextBus.Application.Features.Auth
{
    // 1. Submit User KYC (BVN / NIN via QoreID)
    public record SubmitUserKycCommand(SubmitKycDto Dto) : IRequest<Result<UserDto>>;

    public class SubmitUserKycCommandValidator : AbstractValidator<SubmitUserKycCommand>
    {
        public SubmitUserKycCommandValidator()
        {
            RuleFor(x => x.Dto.BVN)
                .NotEmpty().WithMessage("BVN is required.")
                .Length(11).WithMessage("BVN must be 11 digits.")
                .Must(bvn => bvn.All(char.IsDigit)).WithMessage("BVN must contain only numbers.");

            RuleFor(x => x.Dto.NIN)
                .NotEmpty().WithMessage("NIN is required.")
                .Length(11).WithMessage("NIN must be 11 digits.")
                .Must(nin => nin.All(char.IsDigit)).WithMessage("NIN must contain only numbers.");
        }
    }

    public class SubmitUserKycCommandHandler : IRequestHandler<SubmitUserKycCommand, Result<UserDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IQoreIdService _qoreIdService;
        private readonly IMapper _mapper;

        public SubmitUserKycCommandHandler(
            UserManager<AppUser> userManager,
            ICurrentUserService currentUserService,
            IQoreIdService qoreIdService,
            IMapper mapper)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
            _qoreIdService = qoreIdService;
            _mapper = mapper;
        }

        public async Task<Result<UserDto>> Handle(SubmitUserKycCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<UserDto>.Failure("Unauthorized. Please log in.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<UserDto>.Failure("User not found.");
            }

            // Verify with QoreID Identity Service
            var bvnResult = await _qoreIdService.VerifyBvnAsync(request.Dto.BVN.Trim(), user.Firstname, user.Lastname);
            if (!bvnResult.Succeeded)
            {
                return Result<UserDto>.Failure(bvnResult.Errors);
            }

            var ninResult = await _qoreIdService.VerifyNinAsync(request.Dto.NIN.Trim(), user.Firstname, user.Lastname);
            if (!ninResult.Succeeded)
            {
                return Result<UserDto>.Failure(ninResult.Errors);
            }

            user.BVN = request.Dto.BVN.Trim();
            user.NIN = request.Dto.NIN.Trim();
            user.KycStatus = KycStatus.Verified; // Verified via QoreID clearance
            user.KycSubmittedAt = DateTime.UtcNow;
            user.KycVerifiedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles;
            userDto.IsAdmin = roles.Contains("Admin");

            return Result<UserDto>.Success(userDto, "Identity verified successfully via QoreID.");
        }
    }

    // 2. Submit Driver License (Upload document + Enter License ID number - No QoreID needed)
    public record SubmitDriverLicenseCommand(SubmitDriverLicenseDto Dto) : IRequest<Result<UserDto>>;

    public class SubmitDriverLicenseCommandValidator : AbstractValidator<SubmitDriverLicenseCommand>
    {
        public SubmitDriverLicenseCommandValidator()
        {
            RuleFor(x => x.Dto.DriverLicenseNumber)
                .NotEmpty().WithMessage("Driver's license number is required.")
                .MinimumLength(5).WithMessage("Valid driver's license number must be at least 5 characters.");

            RuleFor(x => x.Dto.DriverLicenseDocumentUrl)
                .NotEmpty().WithMessage("Please upload a photo/scan of your driver's license document.");
        }
    }

    public class SubmitDriverLicenseCommandHandler : IRequestHandler<SubmitDriverLicenseCommand, Result<UserDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public SubmitDriverLicenseCommandHandler(
            UserManager<AppUser> userManager,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<UserDto>> Handle(SubmitDriverLicenseCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<UserDto>.Failure("Unauthorized. Please log in.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<UserDto>.Failure("User not found.");
            }

            user.DriverLicenseNumber = request.Dto.DriverLicenseNumber.Trim().ToUpperInvariant();
            user.DriverLicenseExpiryDate = request.Dto.DriverLicenseExpiryDate;
            user.DriverLicenseDocumentUrl = request.Dto.DriverLicenseDocumentUrl;
            user.DriverKycStatus = DriverKycStatus.Verified;
            user.DriverVerifiedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Driver"))
            {
                await _userManager.AddToRoleAsync(user, "Driver");
                roles.Add("Driver");
            }

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles;
            userDto.IsAdmin = roles.Contains("Admin");

            return Result<UserDto>.Success(userDto, "Driver license submitted and verified successfully. You can now register vehicles and offer rides!");
        }
    }

    // 3. Get Current User Profile
    public record GetCurrentUserQuery : IRequest<Result<UserDto>>;

    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetCurrentUserQueryHandler(
            UserManager<AppUser> userManager,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<UserDto>.Failure("Unauthorized.");
            }

            var user = await _userManager.Users
                .Include(u => u.Wallet)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result<UserDto>.Failure("User not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles;
            userDto.IsAdmin = roles.Contains("Admin");
            userDto.WalletBalance = user.Wallet?.Balance ?? 0m;

            return Result<UserDto>.Success(userDto);
        }
    }
}
