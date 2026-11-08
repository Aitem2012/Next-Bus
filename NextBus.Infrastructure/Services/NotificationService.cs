using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextBus.Application.Common.Interfaces;
using NextBus.Domain.Entities;

namespace NextBus.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IAppDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Notification> CreateNotificationAsync(
            string userId,
            string title,
            string message,
            string type = "Booking",
            string? actionUrl = null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                AppUserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();
                _logger.LogInformation("In-app notification created for user {UserId}: {Title}", userId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist in-app notification for user {UserId}", userId);
            }

            return notification;
        }
    }
}

