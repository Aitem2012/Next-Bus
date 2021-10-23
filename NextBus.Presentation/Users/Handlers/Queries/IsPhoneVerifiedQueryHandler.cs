using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Users.Handlers.Queries
{
    public class IsPhoneVerifiedQueryHandler : IRequestHandler<IsPhoneVerifiedQuery, bool>
     {
          private readonly IAppDbContext _context;
          private readonly UserManager<Domain.Users.AppUser> _userManager;

          public IsPhoneVerifiedQueryHandler(IAppDbContext context, UserManager<Domain.Users.AppUser> userManager)
          {
               _context = context;
               _userManager = userManager;
          }

          public async Task<bool> Handle(IsPhoneVerifiedQuery request, CancellationToken cancellationToken)
          {
               var user = await _context.AppUsers.SingleAsync(e => e.UserName.Equals(request.Username), cancellationToken);

               return await _userManager.IsPhoneNumberConfirmedAsync(user);
          }
     }
}
