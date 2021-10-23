using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Users.Models.Result;
using NextBus.Presentation.Users.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Users.Handlers.Queries
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IEnumerable<GetUserQueryResult>>
     {
          private readonly IAppDbContext _context;
          private readonly IMapper _mapper;
          private readonly IMediator _mediator;

          public GetUsersQueryHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
          {
               _context = context;
               _mapper = mapper;
               _mediator = mediator;
          }

          public async Task<IEnumerable<GetUserQueryResult>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
          {
               var users = await _context.AppUsers.ToListAsync(cancellationToken);
               return _mapper.Map<IEnumerable<GetUserQueryResult>>(users);
          }
     }
}