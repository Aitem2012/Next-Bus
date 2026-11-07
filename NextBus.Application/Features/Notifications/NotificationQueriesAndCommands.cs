using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NextBus.Application.Common.Interfaces;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;

namespace NextBus.Application.Features.Notifications
{
    // 1. Get User Notifications
    public record GetNotificationsQuery : IRequest<Result<NotificationSummaryDto>>;

    public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<NotificationSummaryDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetNotificationsQueryHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<NotificationSummaryDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<NotificationSummaryDto>.Failure("Unauthorized.");
            }

            var notifications = await _context.Notifications
                .Where(n => n.AppUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync(cancellationToken);

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.AppUserId == userId && !n.IsRead, cancellationToken);

            var result = new NotificationSummaryDto
            {
                Notifications = _mapper.Map<List<NotificationDto>>(notifications),
                UnreadCount = unreadCount
            };

            return Result<NotificationSummaryDto>.Success(result);
        }
    }

    // 2. Mark Single Notification as Read
    public record MarkNotificationAsReadCommand(Guid Id) : IRequest<Result>;

    public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Result>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MarkNotificationAsReadCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.Id && n.AppUserId == userId, cancellationToken);

            if (notification == null)
            {
                return Result.Failure("Notification not found.");
            }

            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success("Notification marked as read.");
        }
    }

    // 3. Mark All Notifications as Read
    public record MarkAllNotificationsAsReadCommand : IRequest<Result>;

    public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, Result>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MarkAllNotificationsAsReadCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result.Failure("Unauthorized.");
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.AppUserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
                n.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success("All notifications marked as read.");
        }
    }
}
