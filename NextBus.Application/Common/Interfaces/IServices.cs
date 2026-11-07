using System.Collections.Generic;
using System.Threading.Tasks;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Domain.Entities;
using NextBus.Domain.Enums;

namespace NextBus.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
    }

    public interface IJwtTokenService
    {
        string GenerateToken(AppUser user, IList<string> roles);
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendOtpEmailAsync(string toEmail, string userName, string otpCode, string purpose);
        Task SendBookingAlertEmailAsync(
            string toEmail, 
            string driverName, 
            string passengerName, 
            string passengerPhone, 
            string origin, 
            string destination, 
            string pickup, 
            string dropoff, 
            int seats, 
            decimal fare, 
            System.DateTime departureTime);
    }

    public interface IOtpService
    {
        Task<string> GenerateAndSendOtpAsync(string email, string? userId, OtpPurpose purpose, string? metadata = null);
        Task<Result<OtpVerification>> VerifyOtpAsync(string email, string code, OtpPurpose purpose);
    }

    public interface IPaystackService
    {
        Task<Result<PaystackInitResponseDto>> InitializePaymentAsync(string email, decimal amount, string reference, string callbackUrl);
        Task<Result<PaystackVerifyResponseDto>> VerifyPaymentAsync(string reference);
        Task<Result<PaystackResolveAccountDto>> ResolveAccountNumberAsync(string accountNumber, string bankCode);
        Task<Result<PaystackRecipientResponseDto>> CreateTransferRecipientAsync(string accountName, string accountNumber, string bankCode);
        Task<Result<PaystackTransferResponseDto>> InitiateTransferAsync(decimal amount, string recipientCode, string reason, string reference);
    }

    public interface IQoreIdService
    {
        Task<Result<bool>> VerifyBvnAsync(string bvn, string firstname, string lastname);
        Task<Result<bool>> VerifyNinAsync(string nin, string firstname, string lastname);
    }

    public interface IVehicleOcrService
    {
        Task<Result<VehicleOcrResultDto>> ExtractAndVerifyPlateAsync(string frontPhoto, string backPhoto, string submittedPlate);
    }

    public interface ICloudinaryService
    {
        Task<Result<string>> UploadImageAsync(System.IO.Stream fileStream, string fileName, string folder);
        Task<Result<string>> UploadBase64ImageAsync(string base64Data, string folder, string? fileName = null);
        Task<Result<string>> UploadDocumentAsync(System.IO.Stream fileStream, string fileName, string folder);
        Task<Result<bool>> DeleteAsync(string publicId);
    }
}
