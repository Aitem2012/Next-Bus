using System;

namespace NextBus.Presentation.Transactions.Models.Result
{
    public class DeleteTransactionCommandResult
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public string Message { get; set; }

    }
}
