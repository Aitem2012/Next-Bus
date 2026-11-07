using System;
using NextBus.Domain.Enums;

namespace NextBus.Application.DTOs
{
    public class VehicleDto
    {
        public Guid Id { get; set; }
        public string AppUserId { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; }
        public int TotalSeats { get; set; }
        public bool IsActive { get; set; }

        // 4-Corner Vehicle Photos
        public string FrontPhotoUrl { get; set; } = string.Empty;
        public string BackPhotoUrl { get; set; } = string.Empty;
        public string LeftSidePhotoUrl { get; set; } = string.Empty;
        public string RightSidePhotoUrl { get; set; } = string.Empty;

        // OCR Plate Extraction & Verification
        public bool OcrPlateVerified { get; set; }
        public string OcrExtractedPlateNumber { get; set; } = string.Empty;
        public bool IsApprovedByAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateVehicleDto
    {
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.PrivateCar;
        public int TotalSeats { get; set; }

        // 4-Corner Photos (Required)
        public string FrontPhotoUrl { get; set; } = string.Empty;
        public string BackPhotoUrl { get; set; } = string.Empty;
        public string LeftSidePhotoUrl { get; set; } = string.Empty;
        public string RightSidePhotoUrl { get; set; } = string.Empty;
    }
}
