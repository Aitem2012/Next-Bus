using MediatR;
using NextBus.Presentation.Wallets.Models.Result;

namespace NextBus.Presentation.Wallets.Command
{
    public class AddFundToWalletCommand : IRequest<GetWalletHistoryQueryResult>
     {
          public string AppUserId { get; set; }
          public decimal Amount { get; set; }
     }
}