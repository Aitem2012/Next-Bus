using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Common.Extensions;
using NextBus.Domain.Wallets;
using NextBus.Presentation.Users.Commands;
using NextBus.Presentation.Users.Models.Result;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using NextBus.Presentation.Core;

namespace NextBus.Presentation.Users.Handlers.Commands
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<GetUserQueryResult>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        
        

        private readonly UserManager<Domain.Users.AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<Domain.Users.AppUser> _signInManager;

        public CreateUserCommandHandler(IAppDbContext context, IMapper mapper, IMediator mediator, UserManager<Domain.Users.AppUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<Domain.Users.AppUser> signInManager)
        {
            _context = context;
            _mapper = mapper;
            _mediator = mediator;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            
        }

        public async Task<Result<GetUserQueryResult>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var user = _mapper.Map<Domain.Users.AppUser>(request);

            if (user == null) throw new NotImplementedException();
            user.EmailConfirmed = true;
            user.PhoneNumberConfirmed = false;
           user.UserName = user.Email;
           

            user.Wallet = new Wallet
            {
                Balance = 0,
                Date = DateTime.Now,
                LastUpdated = DateTime.Now,
                Reference = "NxBS".GenerateRef()
            };
            if (await _userManager.FindByEmailAsync(request.Email) != null)
            {
                return Result<GetUserQueryResult>.Failure("Email Exist");
            }

            // Creating User and Adding to Role
            if (_userManager.FindByNameAsync(user.UserName).Result == null)
            {
                var result = await _userManager.CreateAsync(user, request.Password);
                if (result.Succeeded)
                {

                    IdentityRole role = new IdentityRole("User");
                    var isRole = _roleManager.RoleExistsAsync(role.Name).Result;

                    if (!isRole)
                    {
                        var roleResult = _roleManager.CreateAsync(role).Result;

                        if (roleResult.Succeeded)
                        {
                            _userManager.AddToRoleAsync(user, role.Name).Wait();
                        }
                    }
                    else
                    {
                        _userManager.AddToRoleAsync(user, role.Name).Wait();
                    }

                    result = _userManager.AddClaimsAsync(user, new Claim[]{
                              new Claim(ClaimTypes.Role, role.Name),
                              new Claim(ClaimTypes.Name, $"{user.Firstname} {user.Lastname}"),
                              new Claim(ClaimTypes.Email, user.Email)
                              }).Result;
                }
            }

            

            return Result<GetUserQueryResult>.Success(_mapper.Map(user, new GetUserQueryResult()));
        }
    }
}