using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Presentation.Wallets.Models.Result;
using NextBus.Presentation.Wallets.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Wallets.Handlers.Queries
{
    public class GetWalletHistoryQueryHandler : IRequestHandler<GetWalletHistoryQuery, GetWalletHistoryQueryResult>
     {
          private readonly IAppDbContext _context;
          private readonly IMapper _mapper;
          private readonly IMediator _mediator;

          public GetWalletHistoryQueryHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
          {
               _context = context;
               _mapper = mapper;
               _mediator = mediator;
          }

          public async Task<GetWalletHistoryQueryResult> Handle(GetWalletHistoryQuery request, CancellationToken cancellationToken)
          {
               var history = await _context.WalletHistories.SingleAsync(e => e.Id.Equals(request.Id), cancellationToken);
               
               return _mapper.Map<GetWalletHistoryQueryResult>(history);
          }
     }
}