using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Domain.Users;
using NextBus.Presentation.Common.Models.Results;
using NextBus.Presentation.Drivers.Commands;

namespace NextBus.Presentation.Drivers.Handlers.Commands
{
    public class DeleteDriverCommandHandler : IRequestHandler<DeleteDriverCommand, DeleteCommandResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DeleteDriverCommandHandler(IAppDbContext context, IMapper mapper, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<DeleteCommandResult> Handle(DeleteDriverCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.AppUsers.Include(e => e.Driver).Include(e => e.Wallet)
                .ThenInclude(e => e.Histories)
                .SingleAsync(e => e.Id == request.Id, cancellationToken);
            var claims = await _userManager.GetClaimsAsync(user);

            if (claims.Count > 0)
            {
                foreach (var claim in claims)
                {
                    await _userManager.RemoveClaimAsync(user, claim);
                }
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count > 0)
            {
                foreach (var role in roles)
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                }
            }

            await _userManager.DeleteAsync(user);

            _context.Wallets.Remove(user.Wallet);

            await _context.SaveChangesAsync(cancellationToken);

            return new DeleteCommandResult
            {
                IsDeleted = true,
                Id = Guid.Parse(request.Id),
                Message = $"Driver with id {request.Id} has been deleted"
            };
        }
    }
}
