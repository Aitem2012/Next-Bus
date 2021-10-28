using AutoMapper;
using MediatR;
using NextBus.Application.Interfaces.Persistence;
using NextBus.Common.Extensions;
using NextBus.Domain.Wallets;
using NextBus.Presentation.Wallets.Command;
using NextBus.Presentation.Wallets.Models.Result;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Wallets.Handlers.Commands
{
    public class AddFundToWalletCommandHandler : IRequestHandler<AddFundToWalletCommand, GetWalletHistoryQueryResult>
     {
          private readonly IAppDbContext _context;
          private readonly IMapper _mapper;
          private readonly IMediator _mediator;

          public AddFundToWalletCommandHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
          {
               _context = context;
               _mapper = mapper;
               _mediator = mediator;
          }

          public async Task<GetWalletHistoryQueryResult> Handle(AddFundToWalletCommand request, CancellationToken cancellationToken)
          {
               var wallet = _context.Wallets.Single(e => e.AppUserId.Equals(request.AppUserId));
               wallet.Balance = _context.WalletHistories.Where(x => x.WalletAppUserId == request.AppUserId && x.Type == "TOP UP")
                   .Select(x => x.Amount).ToList().Sum();
               var walletHistory = _mapper.Map<WalletHistory>(request);
               walletHistory.Date = DateTime.Now;
               walletHistory.Type = "TOP UP";
               walletHistory.WalletAppUserId = wallet.AppUserId;
               walletHistory.Reference = "NSBS".GenerateRef();
               walletHistory.DateUpdated = DateTime.UtcNow;
               walletHistory.Details = $"{walletHistory.Amount.ToString("N")} Added to Wallet on {walletHistory.Date}";

               wallet.Balance += request.Amount;
               wallet.LastUpdated = DateTime.UtcNow;
               
               _context.Wallets.Attach(wallet);
               await _context.WalletHistories.AddAsync(walletHistory, cancellationToken);
               var result = await _context.SaveChangesAsync(cancellationToken);

               if (result > 0)
               {
                    wallet.Balance += walletHistory.Amount;
                    wallet.LastUpdated = DateTime.Now;

                    
               }

               return _mapper.Map(walletHistory, new GetWalletHistoryQueryResult());
          }
     }
}