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

namespace NextBus.Application.Features.Auth
{
    public record LoginUserCommand(LoginDto Dto) : IRequest<Result<AuthResponseDto>>;

    public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserCommandValidator()
        {
            RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().WithMessage("Email is required.");
            RuleFor(x => x.Dto.Password).NotEmpty().WithMessage("Password is required.");
        }
    }

    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<AuthResponseDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IMapper _mapper;

        public LoginUserCommandHandler(
            UserManager<AppUser> userManager,
            IJwtTokenService jwtTokenService,
            IMapper mapper)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _mapper = mapper;
        }

        public async Task<Result<AuthResponseDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Dto.Email);
            if (user == null)
            {
                return Result<AuthResponseDto>.Failure("Invalid email or password.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Dto.Password);
            if (!passwordValid)
            {
                return Result<AuthResponseDto>.Failure("Invalid email or password.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = roles;
            userDto.IsAdmin = roles.Contains("Admin");
            userDto.WalletBalance = user.Wallet?.Balance ?? 0m;

            return Result<AuthResponseDto>.Success(new AuthResponseDto
            {
                Token = token,
                User = userDto
            }, "Login successful.");
        }
    }
}

