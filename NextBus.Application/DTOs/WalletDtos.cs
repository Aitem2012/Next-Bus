using System;
using System.Collections.Generic;

namespace NextBus.Application.DTOs
{
    public class WalletDto
    {
        public Guid Id { get; set; }
        public string AppUserId { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Reference { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public List<WalletHistoryDto> RecentHistories { get; set; } = [];

        // Escrow & Driver Analytics
        public decimal EscrowHeld { get; set; }
        public decimal PendingDriverEarnings { get; set; }
        public decimal TotalDriverLifetimeEarnings { get; set; }
        public decimal TotalPlatformCommissionPaid { get; set; }
        public int CompletedDriverTrips { get; set; }
        public List<DriverTripEarningDto> DriverEarningsHistory { get; set; } = [];
        public List<PassengerEscrowDto> PassengerEscrowHoldings { get; set; } = [];
    }

    public class DriverTripEarningDto
    {
        public Guid RideId { get; set; }
        public string Route { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int PassengerCount { get; set; }
        public decimal GrossFare { get; set; }
        public decimal CommissionDeducted { get; set; }
        public decimal NetEarnings { get; set; }
        public string Status { get; set; } = "Completed"; // "Completed" or "Pending"
    }

    public class PassengerEscrowDto
    {
        public Guid BookingId { get; set; }
        public Guid RideId { get; set; }
        public string Route { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public int SeatsBooked { get; set; }
        public decimal TotalFare { get; set; }
        public DateTime DepartureTime { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public DateTime HeldAt { get; set; }
    }

    public class WalletHistoryDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class TopUpWalletDto
    {
        public decimal Amount { get; set; }
        public string? PaymentReference { get; set; }
    }

    public class WithdrawFundDto
    {
        public decimal Amount { get; set; }
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
    }
}

