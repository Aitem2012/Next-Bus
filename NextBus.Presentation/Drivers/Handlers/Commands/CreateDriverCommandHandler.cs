using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Common.Extensions;
using NextBus.Domain.Buses;
using NextBus.Domain.Drivers;
using NextBus.Domain.Users;
using NextBus.Domain.Wallets;
using NextBus.Presentation.Core;
using NextBus.Presentation.Drivers.Commands;
using NextBus.Presentation.Drivers.Models.Results;
using NextBus.Presentation.Users.Models.Result;

namespace NextBus.Presentation.Drivers.Handlers.Commands
{
    public class CreateDriverCommandHandler : IRequestHandler<CreateDriverCommand, Result<GetDriverQueryResult>>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CreateDriverCommandHandler(IAppDbContext context, IMapper mapper, UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<GetDriverQueryResult>> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
        {
            var user = _mapper.Map<AppUser>(request.User);

            if (user == null) throw new NotImplementedException();
            //var driver = _mapper.Map<Driver>(request);
            //driver.AppUser = _mapper.Map<AppUser>(request.User);
            
            user.Wallet = new Wallet
            {
                Balance = 0,
                DateCreated = DateTime.Now,
                DateUpdated = DateTime.Now,
                Reference = "NxBS".GenerateRef(),

            };
            user.EmailConfirmed = true;
            user.PhoneNumberConfirmed = false;
            user.UserName = request.User.Email;

            user.Driver = _mapper.Map<Driver>(request);
            user.Driver.Bus.DateCreated = DateTime.UtcNow;
            user.Driver.Bus.DateUpdated = DateTime.UtcNow;
            //driver.Bus = _mapper.Map<Bus>(request.Bus);
            //var user = _mapper.Map<AppUser>(request.User);
            if (await _userManager.FindByEmailAsync(request.User.Email) != null)
            {
                return Result<GetDriverQueryResult>.Failure("Email Exist");
            }

            if (_userManager.FindByEmailAsync(request.User.Email).Result == null)
            {
                var result = await _userManager.CreateAsync(user, request.User.Password);
                if (result.Succeeded)
                {
                    var role = new IdentityRole("Driver");
                    var isRole = _roleManager.RoleExistsAsync(role.Name).Result;
                    if (!isRole)
                    {
                        var roleResult = _roleManager.CreateAsync(role).Result;
                        if (roleResult.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, role.Name);
                        }
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, role.Name);
                    }

                    result = _userManager.AddClaimsAsync(user, new Claim[]
                    {
                        new Claim(ClaimTypes.Role, role.Name),
                        new Claim(ClaimTypes.Name, $"{user.Firstname} {user.Lastname}"),
                        new Claim(ClaimTypes.Email, user.Email)
                    }).Result;
                }

               // var bus = _mapper.Map<Bus>(request.Bus);
               // bus.DateCreated = DateTime.UtcNow;
               // bus.DateUpdated = DateTime.UtcNow;
               // bus.DriverId = user.Id;
               // bus.Driver = user.Driver;
               // //bus.Driver = driver;
               // //driver.AppUserId = user.Id;
               // await _context.Buses.AddAsync(bus, cancellationToken);
               //// await _context.Drivers.AddAsync(driver, cancellationToken);
               // await _context.SaveChangesAsync(cancellationToken);
            }

            return Result<GetDriverQueryResult>.Success(_mapper.Map(user, new GetDriverQueryResult()));
        }
    }
}
