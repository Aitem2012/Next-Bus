namespace NextBus.Application.DTOs
{
    public class MediaUploadResponseDto
    {
        public string Url { get; set; } = string.Empty;
        public string SecureUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public class Base64UploadDto
    {
        public string Base64Data { get; set; } = string.Empty;
        public string Folder { get; set; } = "nextbus/general";
        public string? FileName { get; set; }
    }
}
