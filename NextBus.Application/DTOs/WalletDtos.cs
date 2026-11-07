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

