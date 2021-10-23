using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Users.Handlers.Commands
{
    public class ResetPasswordCodeCommandHandler : IRequestHandler<ResetPasswordCodeCommand, string>
     {
          private readonly IAppDbContext _context;
          private readonly IMapper _mapper;
          private readonly IMediator _mediator;
          private readonly UserManager<Domain.Users.AppUser> _userManager;

          public ResetPasswordCodeCommandHandler(IAppDbContext context, IMapper mapper, IMediator mediator, UserManager<Domain.Users.AppUser> userManager)
          {
               _context = context;
               _mapper = mapper;
               _mediator = mediator;
               _userManager = userManager;
          }

          public async Task<string> Handle(ResetPasswordCodeCommand request, CancellationToken cancellationToken)
          {
               var user = await _userManager.FindByEmailAsync(request.Username);
               return await _userManager.GeneratePasswordResetTokenAsync(user);
          }
     }
}