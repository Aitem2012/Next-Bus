using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using NextBus.Application.Common.Extensions;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;

namespace NextBus.Application.Features.Auth
{
    public record RegisterUserCommand(RegisterDto Dto) : IRequest<Result<AuthResponseDto>>;

    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Dto.Firstname).NotEmpty().WithMessage("First name is required.");
            RuleFor(x => x.Dto.Lastname).NotEmpty().WithMessage("Last name is required.");
            RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
            RuleFor(x => x.Dto.PhoneNumber).NotEmpty().WithMessage("Phone number is required.");
            RuleFor(x => x.Dto.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters.");
        }
    }

    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponseDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IMapper _mapper;

        public RegisterUserCommandHandler(
            UserManager<AppUser> userManager,
            IJwtTokenService jwtTokenService,
            IMapper mapper)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _mapper = mapper;
        }

        public async Task<Result<AuthResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return Result<AuthResponseDto>.Failure("An account with this email already exists.");
            }

            var normalizedPhone = dto.PhoneNumber.NormalizePhoneNumber();

            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Firstname = dto.Firstname.Trim(),
                Lastname = dto.Lastname.Trim(),
                PhoneNumber = normalizedPhone,
                EmailConfirmed = true,
                Wallet = new Wallet
                {
                    Balance = 0m,
                    Reference = "NXBS_W".GenerateRef(),
                    LastUpdated = DateTime.UtcNow
                }
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                return Result<AuthResponseDto>.Failure(createResult.Errors.Select(e => e.Description));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);

            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                Token = token,
                User = _mapper.Map<UserDto>(user)
            }, "User registered successfully.");
        }
    }
}
