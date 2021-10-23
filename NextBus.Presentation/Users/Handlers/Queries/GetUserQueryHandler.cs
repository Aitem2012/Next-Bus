using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Models.Result;
using NextBus.Presentation.Users.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Users.Handlers.Queries
{
    public class GetUserQueryHandler : IRequestHandler<GetUserQuery, GetUserQueryResult>
     {
          private readonly IAppDbContext _context;
          private readonly IMapper _mapper;
          private readonly IMediator _mediator;

          public GetUserQueryHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
          {
               _context = context;
               _mapper = mapper;
               _mediator = mediator;
          }

          public async Task<GetUserQueryResult> Handle(GetUserQuery request, CancellationToken cancellationToken)
          {
               var user = await _context.AppUsers.FirstOrDefaultAsync(e => e.Id.Equals(request.Id), cancellationToken);
               return _mapper.Map<GetUserQueryResult>(user);
          }
     }
}
