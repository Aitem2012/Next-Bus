using MediatR;
using NextBus.Presentation.Transactions.Models.Result;
using System;

namespace NextBus.Presentation.Transactions.Commands
{
    public class CreateTransactionCommand : IRequest<CreateTransactionCommandResult>
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccessful { get; set; }
        public string Reference { get; set; }

        public string AppUserId { get; set; }
        
    }
}
