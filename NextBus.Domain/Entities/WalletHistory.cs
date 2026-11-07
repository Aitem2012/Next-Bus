using System;
using NextBus.Domain.Common;

namespace NextBus.Domain.Entities
{
    public class WalletHistory : BaseEntity
    {
        public Guid WalletId { get; set; }
        public string AppUserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // TopUp, RidePayment, RideEarnings, Refund, Withdrawal
        public string Reference { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public virtual Wallet? Wallet { get; set; }
    }
}
