using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Application.Features.Notifications;

namespace NextBus.API.Controllers
{
    [Authorize]
    public class NotificationsController : BaseApiController
    {
        [HttpGet]
        [ProducesResponseType(typeof(Result<NotificationSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotifications()
        {
            var result = await Mediator.Send(new GetNotificationsQuery());
            return HandleResult(result);
        }

        [HttpPost("{id:guid}/read")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var result = await Mediator.Send(new MarkNotificationAsReadCommand(id));
            return HandleResult(result);
        }

        [HttpPost("read-all")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var result = await Mediator.Send(new MarkAllNotificationsAsReadCommand());
            return HandleResult(result);
        }
    }
}
