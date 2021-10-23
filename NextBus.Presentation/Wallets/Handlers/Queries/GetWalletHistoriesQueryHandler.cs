using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Wallets.Models.Result;
using NextBus.Presentation.Wallets.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Wallets.Handlers.Queries
{
    public class GetWalletHistoriesQueryHandler : IRequestHandler<GetWalletHistoriesQuery, IEnumerable<GetWalletHistoryQueryResult>>
     {
          private readonly IAppDbContext _context;
          private readonly IMediator _mediator;
          private readonly IMapper _mapper;

          public GetWalletHistoriesQueryHandler(IAppDbContext context, IMediator mediator, IMapper mapper)
          {
               _context = context;
               _mediator = mediator;
               _mapper = mapper;
          }

          public async Task<IEnumerable<GetWalletHistoryQueryResult>> Handle(GetWalletHistoriesQuery request, CancellationToken cancellationToken)
          {
               var histories = await _context.WalletHistories.Where(e => e.WalletAppUserId.Equals(request.AppUserId)).ToListAsync(cancellationToken);
               return _mapper.Map<IEnumerable<GetWalletHistoryQueryResult>>(histories);
          }
     }
}