using MediatR;
using NextBus.Presentation.Wallets.Models.Result;
using System.Collections.Generic;

namespace NextBus.Presentation.Wallets.Queries
{
     public class GetWalletsQuery : IRequest<IEnumerable<GetWalletQueryResult>>
     {
     }
}