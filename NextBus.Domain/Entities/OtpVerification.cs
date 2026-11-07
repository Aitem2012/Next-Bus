using System;
using NextBus.Domain.Common;
using NextBus.Domain.Enums;

namespace NextBus.Domain.Entities
{
    public class OtpVerification : BaseEntity
    {
        public string? UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public OtpPurpose Purpose { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; } = false;
        public string? Metadata { get; set; }

        public bool IsValid(string inputCode)
        {
            return !IsUsed && Code == inputCode.Trim() && ExpiryDate >= DateTime.UtcNow;
        }
    }
}

