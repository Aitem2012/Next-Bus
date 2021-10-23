using MediatR;
using NextBus.Presentation.Transactions.Models.Result;
using System.Collections.Generic;

namespace NextBus.Presentation.Transactions.Queries
{
    public class GetTransactionsQuery : IRequest<IEnumerable<GetTransactionQueryResult>>
    {
        
    }
}
