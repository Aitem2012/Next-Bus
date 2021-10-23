using MediatR;
using NextBus.Presentation.Transactions.Models.Result;
using System;

namespace NextBus.Presentation.Transactions.Commands
{
    public class DeleteTransactionCommand : IRequest<DeleteTransactionCommandResult>
    {
        public Guid Id { get; set; }

    }
}
