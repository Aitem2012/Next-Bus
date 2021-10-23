using System;

namespace NextBus.Presentation.Transactions.Models.Result
{
    public class CreateTransactionCommandResult
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Channel { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccessful { get; set; }
        public string Reference { get; set; }
        public string IpAddress { get; set; }
        public string Currency { get; set; }
        public string Vendor { get; set; }
        public string AppUserId { get; set; }
    }
}
