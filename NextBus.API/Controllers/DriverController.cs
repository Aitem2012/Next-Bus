using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Presentation.Common.Models.Results;
using NextBus.Presentation.Drivers.Commands;
using NextBus.Presentation.Drivers.Models.Results;
using NextBus.Presentation.Drivers.Queries;
using NextBus.Presentation.Users.Queries;

namespace NextBus.API.Controllers
{
    public class DriverController : BaseController
    {
        private readonly IMapper _mapper;

        public DriverController(IMapper mapper)
        {
            _mapper = mapper;
        }
        [HttpGet, ProducesResponseType(typeof(IEnumerable<GetDriverQueryResult>), StatusCodes.Status200OK), ProducesDefaultResponseType]
        public async Task<IActionResult> GetDrivers()
        {
            var drivers = await Mediator.Send(new GetDriversQuery());
            return Ok(drivers);
        }
        [HttpGet("{DriverId}", Name = "GetDriver"), ProducesResponseType(typeof(GetDriverQueryResult), StatusCodes.Status200OK), ProducesDefaultResponseType]
        public async Task<IActionResult> GetDriver(string DriverId)
        {
            var Driver = await Mediator.Send(new GetDriverQuery { Id = DriverId });
            return Ok(Driver);
        }
        [HttpPost("CreateDriver", Name = "CreateDriver"), ProducesResponseType(typeof(GetDriverQueryResult), StatusCodes.Status201Created), ProducesDefaultResponseType]
        public async Task<IActionResult> CreateDriver([FromBody] CreateDriverCommand command)
        {
            var result = await Mediator.Send(command);
            //if (!result.IsSuccess)
            //{
            //    return BadRequest(result);
            //}
            return Ok(result);
        }

        [HttpPost(Name = "UpdateDriver"), ProducesResponseType(typeof(GetDriverQueryResult), StatusCodes.Status201Created),
         ProducesDefaultResponseType]
        public async Task<IActionResult> UpdateDriver([FromBody] UpdateDriverCommand command)
        {
            var result = await Mediator.Send(command);
            var Driver = _mapper.Map<GetDriverQueryResult>(result);
            return CreatedAtRoute("GetDriver", new { DriverId = result.Id }, Driver);
        }

        [HttpDelete("{DriverId}", Name = "DeleteDriver"),
         ProducesResponseType(typeof(DeleteCommandResult), StatusCodes.Status410Gone), ProducesDefaultResponseType]
        public async Task<IActionResult> DeleteDriver(string driverId)
        {
            var result = await Mediator.Send(new DeleteDriverCommand { Id = driverId });
            if (!result.IsDeleted)
            {
                return NotFound($"{result.Message}");
            }

            return NoContent();
        }

    }
}
