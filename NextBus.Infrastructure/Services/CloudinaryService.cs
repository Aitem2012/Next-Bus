using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;

namespace NextBus.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;
        private readonly bool _isConfigured;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _logger = logger;

            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (!string.IsNullOrWhiteSpace(cloudName) &&
                !string.IsNullOrWhiteSpace(apiKey) &&
                !string.IsNullOrWhiteSpace(apiSecret))
            {
                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
                _isConfigured = true;
            }
            else
            {
                _logger.LogWarning("Cloudinary credentials are not configured. Uploads will fail or fall back.");
                _cloudinary = new Cloudinary();
                _isConfigured = false;
            }
        }

        public async Task<Result<string>> UploadImageAsync(Stream fileStream, string fileName, string folder)
        {
            if (!_isConfigured)
            {
                return Result<string>.Failure("Cloudinary storage is not properly configured on this environment.");
            }

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = folder,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary image upload error: {Message}", result.Error.Message);
                    return Result<string>.Failure($"Image upload failed: {result.Error.Message}");
                }

                var secureUrl = result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? string.Empty;
                return Result<string>.Success(secureUrl, "Image uploaded successfully to Cloudinary.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while uploading image to Cloudinary for file {FileName}", fileName);
                return Result<string>.Failure($"Image upload exception: {ex.Message}");
            }
        }

        public async Task<Result<string>> UploadBase64ImageAsync(string base64Data, string folder, string? fileName = null)
        {
            if (!_isConfigured)
            {
                return Result<string>.Failure("Cloudinary storage is not properly configured on this environment.");
            }

            if (string.IsNullOrWhiteSpace(base64Data))
            {
                return Result<string>.Failure("Base64 image data cannot be empty.");
            }

            try
            {
                // Clean and normalize base64 or data URI
                string cleanBase64 = base64Data;
                string extension = ".jpg";

                if (base64Data.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(base64Data, @"data:image\/(?<type>.+?);base64,(?<data>.+)");
                    if (match.Success)
                    {
                        var type = match.Groups["type"].Value;
                        extension = $".{type}";
                        cleanBase64 = match.Groups["data"].Value;
                    }
                }

                var bytes = Convert.FromBase64String(cleanBase64);
                using var memoryStream = new MemoryStream(bytes);

                var resolvedFileName = fileName ?? $"img_{Guid.NewGuid():N}{extension}";
                return await UploadImageAsync(memoryStream, resolvedFileName, folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode and upload base64 image to Cloudinary.");
                return Result<string>.Failure($"Failed to process base64 image: {ex.Message}");
            }
        }

        public async Task<Result<string>> UploadDocumentAsync(Stream fileStream, string fileName, string folder)
        {
            if (!_isConfigured)
            {
                return Result<string>.Failure("Cloudinary storage is not properly configured on this environment.");
            }

            try
            {
                var rawName = Path.GetFileNameWithoutExtension(fileName);
                var cleanName = Regex.Replace(rawName, @"[^a-zA-Z0-9_\-]", "_");
                var ext = Path.GetExtension(fileName);
                if (string.IsNullOrWhiteSpace(ext))
                {
                    ext = ".pdf";
                }

                var publicId = $"{cleanName}_{Guid.NewGuid():N}{ext}";

                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = folder,
                    PublicId = publicId,
                    UseFilename = false,
                    UniqueFilename = false,
                    Overwrite = false
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary document upload error: {Message}", result.Error.Message);
                    return Result<string>.Failure($"Document upload failed: {result.Error.Message}");
                }

                var secureUrl = result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? string.Empty;
                return Result<string>.Success(secureUrl, "Document uploaded successfully to Cloudinary.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while uploading document to Cloudinary for {FileName}", fileName);
                return Result<string>.Failure($"Document upload exception: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(string publicId)
        {
            if (!_isConfigured)
            {
                return Result<bool>.Failure("Cloudinary storage is not configured.");
            }

            try
            {
                var imageResult = await _cloudinary.DestroyAsync(
                    new DeletionParams(publicId) { ResourceType = ResourceType.Image });

                if (imageResult.Result == "not found")
                {
                    await _cloudinary.DestroyAsync(
                        new DeletionParams(publicId) { ResourceType = ResourceType.Raw });
                }

                return Result<bool>.Success(true, "Asset deleted from Cloudinary.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception deleting Cloudinary asset {PublicId}", publicId);
                return Result<bool>.Failure($"Failed to delete asset: {ex.Message}");
            }
        }
    }
}
