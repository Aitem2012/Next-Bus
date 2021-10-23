using System;
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
        [HttpGet("{busId}", Name = "GetBus"), ProducesResponseType(typeof(GetUserQueryResult), StatusCodes.Status200OK), ProducesDefaultResponseType]
        public async Task<IActionResult> GetUser(Guid busId)
        {
            var bus = await Mediator.Send(new GetBusQuery{ Id = busId });
            return Ok(bus);
        }
        [HttpPost(Name = "CreateBus"), ProducesResponseType(typeof(GetUserQueryResult), StatusCodes.Status201Created), ProducesDefaultResponseType]
        public async Task<IActionResult> CreateUser([FromBody] CreateBusCommand command)
        {
            var result = await Mediator.Send(command);
            var bus = _mapper.Map<GetBusQueryResult>(result);
            return CreatedAtRoute("GetBus", new { busId = result.Id }, bus);
        }
    }
}
