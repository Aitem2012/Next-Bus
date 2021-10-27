using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Wallets.Models.Result;
using NextBus.Presentation.Wallets.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Wallets.Handlers.Queries
{
    public class GetWalletsQueryHandler : IRequestHandler<GetWalletsQuery, IEnumerable<GetWalletQueryResult>>
     {
          private readonly IAppDbContext _context;
          private readonly IMediator _mediator;
          private readonly IMapper _mapper;

          public GetWalletsQueryHandler(IAppDbContext context, IMediator mediator, IMapper mapper)
          {
               _context = context;
               _mediator = mediator;
               _mapper = mapper;
          }
          public async Task<IEnumerable<GetWalletQueryResult>> Handle(GetWalletsQuery request, CancellationToken cancellationToken)
          {
               var wallets = await _context.Wallets.Include(x => x.AppUser).ToListAsync(cancellationToken);
               

               return _mapper.Map<IEnumerable<GetWalletQueryResult>>(wallets);
          }
     }
}