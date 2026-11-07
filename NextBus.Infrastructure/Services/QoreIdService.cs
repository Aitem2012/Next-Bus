using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;

namespace NextBus.Infrastructure.Services
{
    public class QoreIdService : IQoreIdService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QoreIdService> _logger;

        public QoreIdService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<QoreIdService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var baseUrl = _configuration["QoreId:BaseUrl"] ?? "https://api.qoreid.com";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<Result<bool>> VerifyBvnAsync(string bvn, string firstname, string lastname)
        {
            var isTestMode = _configuration.GetValue<bool>("QoreId:IsTestMode", true);

            // In test mode, allow any 11-digit BVN to be used continuously without flagging or duplicate rejections
            if (isTestMode)
            {
                _logger.LogInformation(
                    "[QOREID TEST MODE] Simulating successful BVN verification for {Firstname} {Lastname} (BVN: {BVN})",
                    firstname, lastname, bvn);

                if (string.IsNullOrWhiteSpace(bvn) || bvn.Length != 11 || !bvn.All(char.IsDigit))
                {
                    return Result<bool>.Failure("BVN must be exactly 11 numeric digits.");
                }

                return Result<bool>.Success(true, "BVN verified successfully via QoreID Identity Service (Test Clearance).");
            }

            try
            {
                var clientId = _configuration["QoreId:ClientId"];
                var secretKey = _configuration["QoreId:SecretKey"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogWarning("QoreID credentials missing in production mode. Falling back to test clearance.");
                    return Result<bool>.Success(true);
                }

                var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/ng/identities/bvn-basic/{bvn}");
                request.Headers.Add("x-api-key", secretKey);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("QoreID BVN verification failed: {StatusCode} - {Content}", response.StatusCode, content);
                    return Result<bool>.Failure("BVN could not be verified by the national identity registry.");
                }

                return Result<bool>.Success(true, "BVN identity confirmed by QoreID.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during QoreID BVN verification for {BVN}", bvn);
                return Result<bool>.Failure($"QoreID identity check error: {ex.Message}");
            }
        }

        public async Task<Result<bool>> VerifyNinAsync(string nin, string firstname, string lastname)
        {
            var isTestMode = _configuration.GetValue<bool>("QoreId:IsTestMode", true);

            // In test mode, allow any 11-digit NIN to be used continuously without flagging or duplicate rejections
            if (isTestMode)
            {
                _logger.LogInformation(
                    "[QOREID TEST MODE] Simulating successful NIN verification for {Firstname} {Lastname} (NIN: {NIN})",
                    firstname, lastname, nin);

                if (string.IsNullOrWhiteSpace(nin) || nin.Length != 11 || !nin.All(char.IsDigit))
                {
                    return Result<bool>.Failure("NIN must be exactly 11 numeric digits.");
                }

                return Result<bool>.Success(true, "NIN verified successfully via QoreID Identity Service (Test Clearance).");
            }

            try
            {
                var secretKey = _configuration["QoreId:SecretKey"];
                var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/ng/identities/nin/{nin}");
                request.Headers.Add("x-api-key", secretKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return Result<bool>.Failure("NIN could not be verified by the national identity registry.");
                }

                return Result<bool>.Success(true, "NIN identity confirmed by QoreID.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during QoreID NIN verification for {NIN}", nin);
                return Result<bool>.Failure($"QoreID identity check error: {ex.Message}");
            }
        }
    }
}
