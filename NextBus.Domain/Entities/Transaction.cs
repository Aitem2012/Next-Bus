using System;
using NextBus.Domain.Common;
using NextBus.Domain.Enums;

namespace NextBus.Domain.Entities
{
    public class Transaction : BaseEntity
    {
        public string AppUserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; } = true;
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public virtual AppUser? AppUser { get; set; }
    }
}
