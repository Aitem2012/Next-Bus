using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Application.Features.Bookings;

namespace NextBus.API.Controllers
{
    [Authorize]
    public class BookingsController : BaseApiController
    {
        [HttpPost]
        [ProducesResponseType(typeof(Result<RideBookingDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RideBookingDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestBooking([FromBody] CreateBookingDto dto)
        {
            var result = await Mediator.Send(new RequestRideBookingCommand(dto));
            return HandleResult(result);
        }

        [HttpPost("{id:guid}/respond")]
        [ProducesResponseType(typeof(Result<RideBookingDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RideBookingDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RespondToBooking(Guid id, [FromBody] RespondBookingDto dto)
        {
            var result = await Mediator.Send(new RespondToBookingCommand(id, dto));
            return HandleResult(result);
        }

        [HttpGet("my-bookings")]
        [ProducesResponseType(typeof(Result<List<RideBookingDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyBookings()
        {
            var result = await Mediator.Send(new GetMyBookingsQuery());
            return HandleResult(result);
        }

        [HttpGet("rides/{rideId:guid}")]
        [ProducesResponseType(typeof(Result<List<RideBookingDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRideBookings(Guid rideId)
        {
            var result = await Mediator.Send(new GetRideBookingsQuery(rideId));
            return HandleResult(result);
        }
    }
}
