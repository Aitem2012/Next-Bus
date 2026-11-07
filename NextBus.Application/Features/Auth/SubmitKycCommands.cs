using System;
using System.Linq;
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
    // 1. Submit User KYC (BVN, NIN)
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
        private readonly IMapper _mapper;

        public SubmitUserKycCommandHandler(
            UserManager<AppUser> userManager,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
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

            user.BVN = request.Dto.BVN;
            user.NIN = request.Dto.NIN;
            user.KycStatus = KycStatus.Verified; // Verified upon BVN/NIN validation
            user.KycSubmittedAt = DateTime.UtcNow;
            user.KycVerifiedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user), "KYC verified successfully.");
        }
    }

    // 2. Submit Driver License
    public record SubmitDriverLicenseCommand(SubmitDriverLicenseDto Dto) : IRequest<Result<UserDto>>;

    public class SubmitDriverLicenseCommandValidator : AbstractValidator<SubmitDriverLicenseCommand>
    {
        public SubmitDriverLicenseCommandValidator()
        {
            RuleFor(x => x.Dto.DriverLicenseNumber).NotEmpty().WithMessage("Driver's license number is required.");
            RuleFor(x => x.Dto.DriverLicenseExpiryDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Driver's license must not be expired.");
            RuleFor(x => x.Dto.DriverLicenseDocumentUrl).NotEmpty().WithMessage("Evidence of driving (license document/image) is required.");
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
            user.DriverKycStatus = DriverKycStatus.Verified; // Approved upon submission of valid license
            user.DriverVerifiedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user), "Driver license registered and verified successfully. You can now offer rides!");
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

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<UserDto>.Failure("User not found.");
            }

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }
    }
}
