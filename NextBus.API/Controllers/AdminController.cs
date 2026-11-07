using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextBus.Application.Features.Admin;

namespace NextBus.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class AdminController : BaseApiController
    {
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var result = await Mediator.Send(new GetAdminOverviewQuery());
            return HandleResult(result);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var result = await Mediator.Send(new GetAdminUsersQuery());
            return HandleResult(result);
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles()
        {
            var result = await Mediator.Send(new GetAdminVehiclesQuery());
            return HandleResult(result);
        }

        [HttpPost("vehicles/{id}/approve")]
        public async Task<IActionResult> ApproveVehicle(Guid id)
        {
            var result = await Mediator.Send(new ApproveVehicleCommand(id));
            return HandleResult(result);
        }

        [HttpPost("kyc/approve/{userId}")]
        public async Task<IActionResult> ApproveDriverKyc(string userId)
        {
            var result = await Mediator.Send(new ApproveDriverKycCommand(userId));
            return HandleResult(result);
        }
    }
}
