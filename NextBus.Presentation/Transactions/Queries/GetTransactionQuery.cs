using MediatR;
using NextBus.Presentation.Transactions.Models.Result;
using System;

namespace NextBus.Presentation.Transactions.Queries
{
    public class GetTransactionQuery : IRequest<GetTransactionQueryResult>
    {
        public Guid Id { get; set; }

    }
}
