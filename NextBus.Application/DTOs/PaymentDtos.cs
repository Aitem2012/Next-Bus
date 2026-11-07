using System;

namespace NextBus.Application.DTOs
{
    public class PaystackInitResponseDto
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string AccessCode { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    public class PaystackVerifyResponseDto
    {
        public bool IsSuccessful { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Currency { get; set; } = "NGN";
        public string CustomerEmail { get; set; } = string.Empty;
    }

    public class PaystackResolveAccountDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int BankId { get; set; }
    }

    public class PaystackRecipientResponseDto
    {
        public string RecipientCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class PaystackTransferResponseDto
    {
        public string TransferCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    public class InitializeFundWalletDto
    {
        public decimal Amount { get; set; }
        public string? CallbackUrl { get; set; }
    }

    public class VerifyFundWalletDto
    {
        public string Reference { get; set; } = string.Empty;
    }

    public class RequestWithdrawalDto
    {
        public decimal Amount { get; set; }
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
    }

    public class ConfirmWithdrawalDto
    {
        public string OtpCode { get; set; } = string.Empty;
    }

    public class VerifyEmailOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }

    public class RequestPasswordResetDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordWithOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}

