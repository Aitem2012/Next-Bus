using MediatR;
using NextBus.Presentation.Wallets.Models.Result;
using System;

namespace NextBus.Presentation.Wallets.Queries
{
     public class GetWalletHistoryQuery : IRequest<GetWalletHistoryQueryResult>
     {
          public Guid Id { get; set; }
     }
}