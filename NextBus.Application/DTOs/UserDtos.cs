using System;
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
        public DateTime DriverLicenseExpiryDate { get; set; }
        public string DriverLicenseDocumentUrl { get; set; } = string.Empty;
    }
}
