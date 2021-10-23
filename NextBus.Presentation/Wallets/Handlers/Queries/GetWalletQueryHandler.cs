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
    public class GetWalletQueryHandler : IRequestHandler<GetWalletQuery, GetWalletQueryResult>
     {
          private readonly IAppDbContext _context;
          private readonly IMapper _mapper;
          private readonly IMediator _mediator;

          public GetWalletQueryHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
          {
               _context = context;
               _mapper = mapper;
               _mediator = mediator;
          }

          public async Task<GetWalletQueryResult> Handle(GetWalletQuery request, CancellationToken cancellationToken)
          {
               var wallet = await _context.Wallets.SingleAsync(e => e.AppUserId.Equals(request.AppUserId), cancellationToken);

               return _mapper.Map<GetWalletQueryResult>(wallet);
          }
     }
}