using MediatR;
using NextBus.Presentation.Wallets.Models.Result;

namespace NextBus.Presentation.Wallets.Queries
{
     public class GetWalletQuery : IRequest<GetWalletQueryResult>
     {
          public string AppUserId { get; set; }
     }
}