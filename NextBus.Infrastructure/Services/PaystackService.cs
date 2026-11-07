using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Application.Common.Interfaces;

namespace NextBus.Infrastructure.Services
{
    public class PaystackService : IPaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaystackService> _logger;
        private readonly string? _secretKey;

        public PaystackService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PaystackService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _secretKey = _configuration["Paystack:SecretKey"];

            _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
            if (!string.IsNullOrEmpty(_secretKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _secretKey);
            }
        }

        public async Task<Result<PaystackInitResponseDto>> InitializePaymentAsync(
            string email, decimal amount, string reference, string callbackUrl)
        {
            // If secret key is not configured, simulate successful Paystack authorization URL
            if (string.IsNullOrEmpty(_secretKey) || _secretKey.Contains("YOUR_"))
            {
                _logger.LogInformation("[PAYSTACK SIMULATION] Initializing payment: {Email}, Amount: {Amount}, Ref: {Ref}",
                    email, amount, reference);

                return Result<PaystackInitResponseDto>.Success(new PaystackInitResponseDto
                {
                    AuthorizationUrl = $"https://checkout.paystack.com/simulate/{reference}",
                    AccessCode = $"acc_{Guid.NewGuid():N}"[..12],
                    Reference = reference
                });
            }

            try
            {
                var payload = new
                {
                    email,
                    amount = (long)(amount * 100), // Paystack accepts amount in kobo
                    reference,
                    callback_url = callbackUrl
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("transaction/initialize", content);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp) && statusProp.GetBoolean())
                {
                    var data = root.GetProperty("data");
                    return Result<PaystackInitResponseDto>.Success(new PaystackInitResponseDto
                    {
                        AuthorizationUrl = data.GetProperty("authorization_url").GetString() ?? string.Empty,
                        AccessCode = data.GetProperty("access_code").GetString() ?? string.Empty,
                        Reference = data.GetProperty("reference").GetString() ?? reference
                    });
                }

                var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Paystack initialization failed";
                return Result<PaystackInitResponseDto>.Failure(message ?? "Payment initialization failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack InitializePaymentAsync error");
                return Result<PaystackInitResponseDto>.Failure($"Paystack error: {ex.Message}");
            }
        }

        public async Task<Result<PaystackVerifyResponseDto>> VerifyPaymentAsync(string reference)
        {
            if (string.IsNullOrEmpty(_secretKey) || _secretKey.Contains("YOUR_"))
            {
                _logger.LogInformation("[PAYSTACK SIMULATION] Verifying payment reference: {Ref}", reference);
                return Result<PaystackVerifyResponseDto>.Success(new PaystackVerifyResponseDto
                {
                    IsSuccessful = true,
                    Amount = 1000m,
                    Reference = reference,
                    Channel = "card",
                    Currency = "NGN",
                    CustomerEmail = "customer@example.com"
                });
            }

            try
            {
                var response = await _httpClient.GetAsync($"transaction/verify/{reference}");
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp) && statusProp.GetBoolean())
                {
                    var data = root.GetProperty("data");
                    var paymentStatus = data.GetProperty("status").GetString();
                    var amountInKobo = data.GetProperty("amount").GetDecimal();
                    var customer = data.GetProperty("customer");

                    return Result<PaystackVerifyResponseDto>.Success(new PaystackVerifyResponseDto
                    {
                        IsSuccessful = paymentStatus == "success",
                        Amount = amountInKobo / 100m,
                        Reference = reference,
                        Channel = data.TryGetProperty("channel", out var ch) ? ch.GetString() ?? "card" : "card",
                        Currency = data.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "NGN" : "NGN",
                        CustomerEmail = customer.TryGetProperty("email", out var em) ? em.GetString() ?? "" : ""
                    });
                }

                var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : "Verification failed";
                return Result<PaystackVerifyResponseDto>.Failure(message ?? "Payment verification failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack VerifyPaymentAsync error");
                return Result<PaystackVerifyResponseDto>.Failure($"Paystack verification error: {ex.Message}");
            }
        }

        public async Task<Result<PaystackResolveAccountDto>> ResolveAccountNumberAsync(string accountNumber, string bankCode)
        {
            if (string.IsNullOrEmpty(_secretKey) || _secretKey.Contains("YOUR_"))
            {
                return Result<PaystackResolveAccountDto>.Success(new PaystackResolveAccountDto
                {
                    AccountNumber = accountNumber,
                    AccountName = "Simulated Account Holder",
                    BankId = 1
                });
            }

            try
            {
                var response = await _httpClient.GetAsync($"bank/resolve?account_number={accountNumber}&bank_code={bankCode}");
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp) && statusProp.GetBoolean())
                {
                    var data = root.GetProperty("data");
                    return Result<PaystackResolveAccountDto>.Success(new PaystackResolveAccountDto
                    {
                        AccountNumber = data.GetProperty("account_number").GetString() ?? accountNumber,
                        AccountName = data.GetProperty("account_name").GetString() ?? string.Empty,
                        BankId = data.TryGetProperty("bank_id", out var bId) ? bId.GetInt32() : 0
                    });
                }

                var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : "Unable to resolve account";
                return Result<PaystackResolveAccountDto>.Failure(message ?? "Account resolution failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack ResolveAccountNumberAsync error");
                return Result<PaystackResolveAccountDto>.Failure($"Paystack error: {ex.Message}");
            }
        }

        public async Task<Result<PaystackRecipientResponseDto>> CreateTransferRecipientAsync(
            string accountName, string accountNumber, string bankCode)
        {
            if (string.IsNullOrEmpty(_secretKey) || _secretKey.Contains("YOUR_"))
            {
                return Result<PaystackRecipientResponseDto>.Success(new PaystackRecipientResponseDto
                {
                    RecipientCode = $"RCP_{Guid.NewGuid():N}"[..12],
                    Name = accountName
                });
            }

            try
            {
                var payload = new
                {
                    type = "nuban",
                    name = accountName,
                    account_number = accountNumber,
                    bank_code = bankCode,
                    currency = "NGN"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("transferrecipient", content);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp) && statusProp.GetBoolean())
                {
                    var data = root.GetProperty("data");
                    return Result<PaystackRecipientResponseDto>.Success(new PaystackRecipientResponseDto
                    {
                        RecipientCode = data.GetProperty("recipient_code").GetString() ?? string.Empty,
                        Name = data.GetProperty("name").GetString() ?? accountName
                    });
                }

                var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : "Failed to create recipient";
                return Result<PaystackRecipientResponseDto>.Failure(message ?? "Transfer recipient creation failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack CreateTransferRecipientAsync error");
                return Result<PaystackRecipientResponseDto>.Failure($"Paystack error: {ex.Message}");
            }
        }

        public async Task<Result<PaystackTransferResponseDto>> InitiateTransferAsync(
            decimal amount, string recipientCode, string reason, string reference)
        {
            if (string.IsNullOrEmpty(_secretKey) || _secretKey.Contains("YOUR_"))
            {
                _logger.LogInformation("[PAYSTACK SIMULATION] Transfer initiated: {Amount} NGN to {RecipientCode}", amount, recipientCode);
                return Result<PaystackTransferResponseDto>.Success(new PaystackTransferResponseDto
                {
                    TransferCode = $"TRF_{Guid.NewGuid():N}"[..12],
                    Status = "success",
                    Reference = reference
                });
            }

            try
            {
                var payload = new
                {
                    source = "balance",
                    amount = (long)(amount * 100),
                    recipient = recipientCode,
                    reason,
                    reference
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("transfer", content);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp) && statusProp.GetBoolean())
                {
                    var data = root.GetProperty("data");
                    return Result<PaystackTransferResponseDto>.Success(new PaystackTransferResponseDto
                    {
                        TransferCode = data.GetProperty("transfer_code").GetString() ?? string.Empty,
                        Status = data.GetProperty("status").GetString() ?? "success",
                        Reference = reference
                    });
                }

                var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : "Transfer failed";
                return Result<PaystackTransferResponseDto>.Failure(message ?? "Transfer failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack InitiateTransferAsync error");
                return Result<PaystackTransferResponseDto>.Failure($"Paystack transfer error: {ex.Message}");
            }
        }
    }
}

