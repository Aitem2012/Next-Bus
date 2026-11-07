using System;
using System.Collections.Generic;
using NextBus.Domain.Enums;

namespace NextBus.Application.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? BVN { get; set; }
        public string? NIN { get; set; }
        public KycStatus KycStatus { get; set; }
        public bool IsKycVerified { get; set; }

        // Driver details
        public DriverKycStatus DriverKycStatus { get; set; }
        public string? DriverLicenseNumber { get; set; }
        public DateTime? DriverLicenseExpiryDate { get; set; }
        public string? DriverLicenseDocumentUrl { get; set; }
        public bool CanOfferRides { get; set; }

        // Role & Administrative privileges
        public bool IsAdmin { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public decimal WalletBalance { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }

    public class RegisterDto
    {
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsDriver { get; set; } = false;
        public bool IsPassenger { get; set; } = true;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SubmitKycDto
    {
        public string BVN { get; set; } = string.Empty;
        public string NIN { get; set; } = string.Empty;
    }

    public class SubmitDriverLicenseDto
    {
        public string DriverLicenseNumber { get; set; } = string.Empty;
        public DateTime? DriverLicenseExpiryDate { get; set; }
        public string DriverLicenseDocumentUrl { get; set; } = string.Empty;
    }

    public class AdminOverviewDto
    {
        public int TotalUsers { get; set; }
        public int TotalPassengers { get; set; }
        public int TotalDrivers { get; set; }
        public int TotalVehicles { get; set; }
        public int PendingVehicles { get; set; }
        public int TotalRides { get; set; }
        public int ActiveRides { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalEscrowHeld { get; set; }
        public decimal TotalPlatformCommission { get; set; }
    }

    public class VehicleOcrResultDto
    {
        public bool IsMatch { get; set; }
        public string ExtractedPlateNumber { get; set; } = string.Empty;
        public string ConfidenceScore { get; set; } = "98.4%";
        public string Details { get; set; } = string.Empty;
    }
}
