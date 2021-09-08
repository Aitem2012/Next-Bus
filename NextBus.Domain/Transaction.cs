using System;

namespace NextBus.Domain
{
    public class Transaction
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccessful { get; set; }
        public string Reference { get; set; }
        
        public string AppUserId { get; set; }

        
        // One to Many Transaction Relationship
        public virtual AppUser AppUser { get; set; }
    }
}
