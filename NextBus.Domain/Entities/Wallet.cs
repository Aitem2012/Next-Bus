using System;
using System.Collections.Generic;
using NextBus.Domain.Common;

namespace NextBus.Domain.Entities
{
    public class Wallet : BaseEntity
    {
        public string AppUserId { get; set; } = string.Empty;
        public decimal Balance { get; set; } = 0m;
        public string Reference { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public virtual AppUser? AppUser { get; set; }
        public virtual ICollection<WalletHistory> Histories { get; set; } = new List<WalletHistory>();
    }
}
