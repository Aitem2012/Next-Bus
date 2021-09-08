using System;
using Microsoft.AspNetCore.Identity;

namespace NextBus.Domain
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

        public virtual Wallet Wallet { get; set; }

    }
}
