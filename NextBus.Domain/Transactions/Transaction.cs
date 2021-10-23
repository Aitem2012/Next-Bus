﻿using System;
using NextBus.Domain.BaseEntities;
using NextBus.Domain.Users;

namespace NextBus.Domain.Transactions
{
    public class Transaction : BaseEntity
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
