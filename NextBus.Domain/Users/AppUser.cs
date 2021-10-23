using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using NextBus.Domain.Drivers;
using NextBus.Domain.Wallets;
using NextBus.Domain.Transactions;

namespace NextBus.Domain.Users
{
    public class AppUser : IdentityUser
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string BVN { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public string PickupPoint { get; set; }
        public string Destination { get; set; }
        public virtual Driver Driver { get; set; }
        public virtual Wallet Wallet { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; } = new HashSet<Transaction>();


    }
}
