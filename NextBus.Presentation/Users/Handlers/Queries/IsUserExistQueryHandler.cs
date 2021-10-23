using MediatR;
using Microsoft.AspNetCore.Identity;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Users.Handlers.Queries
{
    public class IsUserExistQueryHandler : IRequestHandler<IsUserExistQuery, bool>
     {
          private readonly IAppDbContext _context;
          private readonly UserManager<Domain.Users.AppUser> _userManager;

          public IsUserExistQueryHandler(IAppDbContext context, UserManager<Domain.Users.AppUser> userManager)
          {
               _context = context;
               _userManager = userManager;
          }

          public async Task<bool> Handle(IsUserExistQuery request, CancellationToken cancellationToken)
          {
               if (await _userManager.FindByNameAsync(request.Username) == null)
               {
                    return false;
               }
               return true;
          }
     }
}
