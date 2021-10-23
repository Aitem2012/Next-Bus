using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Commands;
using NextBus.Presentation.Users.Models.Result;
using System.Threading;
using System.Threading.Tasks;
using NextBus.Domain.Users;

namespace NextBus.Presentation.Users.Handlers.Commands
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, DeleteUserCommandResult>
     {

          private readonly IAppDbContext _context;
          private readonly IMediator _mediator;
          private readonly UserManager<Domain.Users.AppUser> _userManager;

          public DeleteUserCommandHandler(IAppDbContext context, IMediator mediator, UserManager<Domain.Users.AppUser> userManager)
          {
               _context = context;
               _mediator = mediator;
               _userManager = userManager;
          }

          public async Task<DeleteUserCommandResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
          {
               var user = await _context.AppUsers.Include(e => e.Wallet).ThenInclude(e => e.Histories).FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

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
               

               return new DeleteUserCommandResult
               {
                    Id = request.Id,
                    IsDeleted = true,
                    Message = $"User with Id {request.Id} is deleted successfully"
               };
          }
     }
}
