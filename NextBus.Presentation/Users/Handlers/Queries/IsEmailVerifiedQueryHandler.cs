using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Users.Handlers.Queries
{
    public class IsEmailVerifiedQueryHandler : IRequestHandler<IsEmailVerifiedQuery, bool>
     {
          private readonly IAppDbContext _context;
          private readonly UserManager<Domain.Users.AppUser> _userManager;

          public IsEmailVerifiedQueryHandler(IAppDbContext context, UserManager<Domain.Users.AppUser> userManager)
          {
               _context = context;
               _userManager = userManager;
          }

          public async Task<bool> Handle(IsEmailVerifiedQuery request, CancellationToken cancellationToken)
          {
               var user = await _context.AppUsers.SingleAsync(e => e.UserName.Equals(request.Username), cancellationToken);

               return await _userManager.IsEmailConfirmedAsync(user);
          }
     }
}