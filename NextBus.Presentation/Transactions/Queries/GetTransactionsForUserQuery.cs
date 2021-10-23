using MediatR;
using NextBus.Presentation.Transactions.Models.Result;
using System.Collections.Generic;

namespace NextBus.Presentation.Transactions.Queries
{
    public class GetTransactionsForUserQuery : IRequest<IEnumerable<GetTransactionQueryResult>>
    {
        public string AppUserId { get; set; }

    }
}
