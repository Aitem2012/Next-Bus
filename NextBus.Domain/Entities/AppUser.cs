using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using NextBus.Domain.Enums;

namespace NextBus.Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string? BVN { get; set; }
        public string? NIN { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // General KYC (Required for passengers & drivers)
        public KycStatus KycStatus { get; set; } = KycStatus.NotSubmitted;
        public DateTime? KycSubmittedAt { get; set; }
        public DateTime? KycVerifiedAt { get; set; }

        // Driver Verification & License (Required if offering rides)
        public string? DriverLicenseNumber { get; set; }
        public DateTime? DriverLicenseExpiryDate { get; set; }
        public string? DriverLicenseDocumentUrl { get; set; }
        public DriverKycStatus DriverKycStatus { get; set; } = DriverKycStatus.NotSubmitted;
        public DateTime? DriverVerifiedAt { get; set; }

        public bool CanOfferRides => DriverKycStatus == DriverKycStatus.Verified;
        public bool IsKycVerified => KycStatus == KycStatus.Verified;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Wallet? Wallet { get; set; }
        public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public virtual ICollection<Ride> OfferedRides { get; set; } = new List<Ride>();
        public virtual ICollection<RideBooking> Bookings { get; set; } = new List<RideBooking>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
