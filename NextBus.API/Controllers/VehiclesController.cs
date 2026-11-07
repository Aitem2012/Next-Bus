using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Application.Features.Vehicles;

namespace NextBus.API.Controllers
{
    [Authorize]
    public class VehiclesController : BaseApiController
    {
        [HttpPost]
        [ProducesResponseType(typeof(Result<VehicleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<VehicleDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterVehicle([FromBody] CreateVehicleDto dto)
        {
            var result = await Mediator.Send(new RegisterVehicleCommand(dto));
            return HandleResult(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<List<VehicleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyVehicles()
        {
            var result = await Mediator.Send(new GetMyVehiclesQuery());
            return HandleResult(result);
        }
    }
}

