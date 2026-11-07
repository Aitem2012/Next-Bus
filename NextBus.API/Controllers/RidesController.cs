using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Application.Features.Rides;

namespace NextBus.API.Controllers
{
    public class RidesController : BaseApiController
    {
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(Result<RideDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RideDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRide([FromBody] CreateRideDto dto)
        {
            var result = await Mediator.Send(new CreateRideCommand(dto));
            return HandleResult(result);
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(Result<List<RideDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchRides([FromQuery] RideSearchQueryDto criteria)
        {
            var result = await Mediator.Send(new SearchRidesQuery(criteria));
            return HandleResult(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<RideDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RideDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRideById(Guid id)
        {
            var result = await Mediator.Send(new GetRideByIdQuery(id));
            return HandleResult(result);
        }

        [Authorize]
        [HttpGet("my-offered-rides")]
        [ProducesResponseType(typeof(Result<List<RideDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyOfferedRides()
        {
            var result = await Mediator.Send(new GetMyOfferedRidesQuery());
            return HandleResult(result);
        }

        [Authorize]
        [HttpPost("{id:guid}/complete")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteRide(Guid id)
        {
            var result = await Mediator.Send(new CompleteRideCommand(id));
            return HandleResult(result);
        }
    }
}
