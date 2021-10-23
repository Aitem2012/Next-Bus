using System;
using System.Collections.Generic;
using NextBus.Domain.BaseEntities;
using NextBus.Domain.Users;

namespace NextBus.Domain.Wallets
{
    public class Wallet : BaseEntity
    {
        public Wallet()
        {
            Histories = new HashSet<WalletHistory>();
        }

        public string AppUserId { get; set; }
        public decimal Balance { get; set; }
        public DateTime Date { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Reference { get; set; }
        

        // One to Zero or One Wallet Relationship
        public virtual AppUser AppUser { get; set; }

        // One Wallet to Many Relationships
        public virtual ICollection<WalletHistory> Histories { get; set; }
    }
}