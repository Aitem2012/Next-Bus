using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;

namespace NextBus.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;

        public MediaController(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        /// <summary>
        /// Upload an image file directly to Cloudinary (e.g. 4-corner vehicle photos, profile pictures).
        /// </summary>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(Result<MediaUploadResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<MediaUploadResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string? folder = "nextbus/vehicles")
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(Result<MediaUploadResponseDto>.Failure("No file was uploaded or file is empty."));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, ext) < 0)
            {
                return BadRequest(Result<MediaUploadResponseDto>.Failure("Only image files (.jpg, .jpeg, .png, .webp) are allowed."));
            }

            using var stream = file.OpenReadStream();
            var targetFolder = string.IsNullOrWhiteSpace(folder) ? "nextbus/vehicles" : folder;
            var result = await _cloudinaryService.UploadImageAsync(stream, file.FileName, targetFolder);

            if (!result.Succeeded || string.IsNullOrEmpty(result.Data))
            {
                return BadRequest(Result<MediaUploadResponseDto>.Failure(result.Errors));
            }

            var response = new MediaUploadResponseDto
            {
                Url = result.Data,
                SecureUrl = result.Data,
                FileName = file.FileName
            };

            return Ok(Result<MediaUploadResponseDto>.Success(response, "Image uploaded successfully to Cloudinary."));
        }

        /// <summary>
        /// Upload a base64 encoded image or Data URI to Cloudinary.
        /// </summary>
        [HttpPost("upload-base64")]
        [ProducesResponseType(typeof(Result<MediaUploadResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<MediaUploadResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadBase64([FromBody] Base64UploadDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Base64Data))
            {
                return BadRequest(Result<MediaUploadResponseDto>.Failure("Base64Data is required."));
            }

            var targetFolder = string.IsNullOrWhiteSpace(dto.Folder) ? "nextbus/vehicles" : dto.Folder;
            var result = await _cloudinaryService.UploadBase64ImageAsync(dto.Base64Data, targetFolder, dto.FileName);

            if (!result.Succeeded || string.IsNullOrEmpty(result.Data))
            {
                return BadRequest(Result<MediaUploadResponseDto>.Failure(result.Errors));
            }

            var response = new MediaUploadResponseDto
            {
                Url = result.Data,
                SecureUrl = result.Data,
                FileName = dto.FileName ?? "upload.jpg"
            };

            return Ok(Result<MediaUploadResponseDto>.Success(response, "Base64 image uploaded successfully to Cloudinary."));
        }

        /// <summary>
        /// Upload a driver license or verification document (PDF, PNG, JPG) to Cloudinary.
        /// </summary>
        [HttpPost("upload-document")]
        [ProducesResponseType(typeof(Result<MediaUploadResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<MediaUploadResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string? folder = "nextbus/licenses")
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(Result<MediaUploadResponseDto>.Failure("No file was uploaded or file is empty."));
            }

            using var stream = file.OpenReadStream();
            var targetFolder = string.IsNullOrWhiteSpace(folder) ? "nextbus/licenses" : folder;
            var result = await _cloudinaryService.UploadDocumentAsync(stream, file.FileName, targetFolder);

            if (!result.Succeeded || string.IsNullOrEmpty(result.Data))
            {
                return BadRequest(Result<MediaUploadResponseDto>.Failure(result.Errors));
            }

            var response = new MediaUploadResponseDto
            {
                Url = result.Data,
                SecureUrl = result.Data,
                FileName = file.FileName
            };

            return Ok(Result<MediaUploadResponseDto>.Success(response, "Document uploaded successfully to Cloudinary."));
        }
    }
}
