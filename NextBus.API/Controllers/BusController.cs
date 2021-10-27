using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Presentation.Buses.Commands;
using NextBus.Presentation.Buses.Models.Result;
using NextBus.Presentation.Buses.Queries;
using NextBus.Presentation.Users.Commands;
using NextBus.Presentation.Users.Models.Result;
using NextBus.Presentation.Users.Queries;

namespace NextBus.API.Controllers
{
    public class BusController : BaseController
    {
        private readonly IMapper _mapper;

        public BusController(IMapper mapper)
        {
            _mapper = mapper;
        }
        [HttpGet, ProducesResponseType(typeof(IEnumerable<GetBusQueryResult>), StatusCodes.Status200OK), ProducesDefaultResponseType]
        public async Task<IActionResult> GetBuses()
        {
            var buses = await Mediator.Send(new GetBusesQuery());
            return Ok(buses);
        }
        [HttpGet("{busId}", Name = "GetBus"), ProducesResponseType(typeof(GetBusQueryResult), StatusCodes.Status200OK), ProducesDefaultResponseType]
        public async Task<IActionResult> GetBus(Guid busId)
        {
            var bus = await Mediator.Send(new GetBusQuery{ Id = busId });
            return Ok(bus);
        }
        [HttpPost(Name = "CreateBus"), ProducesResponseType(typeof(GetBusQueryResult), StatusCodes.Status201Created), ProducesDefaultResponseType]
        public async Task<IActionResult> CreateBus([FromBody] CreateBusCommand command)
        {
            var result = await Mediator.Send(command);
            var bus = _mapper.Map<GetBusQueryResult>(result);
            return CreatedAtRoute("GetBus", new { busId = result.Id }, bus);
        }
        [HttpPost("{busId}", Name = "UpdateBus"), ProducesResponseType(typeof(GetBusQueryResult), StatusCodes.Status201Created),
         ProducesDefaultResponseType]
        public async Task<IActionResult> UpdateBus([FromBody] UpdateBusCommand command)
        {
            var result = await Mediator.Send(command);
            var bus = _mapper.Map<GetBusQueryResult>(result);
            return CreatedAtRoute("GetBus", new { busId = result.Id }, bus);
        }

        [HttpDelete("{busId}", Name = "DeleteBus"),
         ProducesResponseType(typeof(DeleteUserCommandResult), StatusCodes.Status410Gone), ProducesDefaultResponseType]
        public async Task<IActionResult> DeleteBus(Guid busId)
        {
            var result = await Mediator.Send(new DeleteBusCommand { Id = busId });
            if (!result.IsDeleted)
            {
                return NotFound($"{result.Message}");
            }
            return NoContent();
        }
    }
}
