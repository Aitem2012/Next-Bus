using System;
using System.Collections.Generic;
using NextBus.Domain.Common;
using NextBus.Domain.Enums;

namespace NextBus.Domain.Entities
{
    public class Vehicle : BaseEntity
    {
        public string AppUserId { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Color { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.PrivateCar;
        public int TotalSeats { get; set; }
        public bool IsActive { get; set; } = true;

        // 4-Corner Vehicle Photos for Driver Verification & Safety
        public string FrontPhotoUrl { get; set; } = string.Empty;       // Front corner showing license plate
        public string BackPhotoUrl { get; set; } = string.Empty;        // Rear corner showing license plate
        public string LeftSidePhotoUrl { get; set; } = string.Empty;    // Left side corner
        public string RightSidePhotoUrl { get; set; } = string.Empty;   // Right side corner

        // Optical Character Recognition (OCR) plate matching
        public bool OcrPlateVerified { get; set; } = false;
        public string OcrExtractedPlateNumber { get; set; } = string.Empty;
        public bool IsApprovedByAdmin { get; set; } = true;

        public virtual AppUser? AppUser { get; set; }
        public virtual ICollection<Ride> Rides { get; set; } = new List<Ride>();
    }
}

