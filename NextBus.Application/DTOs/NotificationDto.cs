using System;

namespace NextBus.Application.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string AppUserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Booking";
        public bool IsRead { get; set; }
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationSummaryDto
    {
        public System.Collections.Generic.List<NotificationDto> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}

