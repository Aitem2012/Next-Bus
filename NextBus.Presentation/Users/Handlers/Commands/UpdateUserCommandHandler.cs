using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Commands;
using NextBus.Presentation.Users.Models.Result;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Users.Handlers.Commands
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, GetUserQueryResult>
     {
          private readonly IAppDbContext _context;
          private readonly IMapper _mapper;
          private readonly IMediator _mediator;

          public UpdateUserCommandHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
          {
               _context = context;
               _mapper = mapper;
               _mediator = mediator;
          }

          public async Task<GetUserQueryResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
          {
               var userFromDb = await _context.AppUsers.SingleAsync(e => e.Id.Equals(request.Id), cancellationToken);
               var user = _mapper.Map(request, userFromDb);

               _context.AppUsers.Attach(user);
               await _context.SaveChangesAsync(cancellationToken);
               
               return _mapper.Map(user, new GetUserQueryResult());
          }
     }
}