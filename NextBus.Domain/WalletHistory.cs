using System;

namespace NextBus.Domain
{
    public class WalletHistory : BaseEntity
    {
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        public string Details { get; set; }
        public string WalletAppUserId { get; set; }
        
        // One to Many WalletHistories Relationships
        public virtual Wallet Wallet { get; set; }
    }
}