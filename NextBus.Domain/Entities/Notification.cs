using System;
using NextBus.Domain.Common;

namespace NextBus.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string AppUserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Booking"; // "Booking", "Ride", "Wallet", "System"
        public bool IsRead { get; set; } = false;
        public string? ActionUrl { get; set; }

        public virtual AppUser? AppUser { get; set; }
    }
}
