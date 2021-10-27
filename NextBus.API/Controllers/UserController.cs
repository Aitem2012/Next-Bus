using System.Collections.Generic;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Presentation.Users.Commands;
using NextBus.Presentation.Users.Models.Result;
using NextBus.Presentation.Users.Queries;
using System.Threading.Tasks;

namespace NextBus.API.Controllers
{
    public class UserController : BaseController
    {
        private readonly IMapper _mapper;

        public UserController(IMapper mapper)
        {
            _mapper = mapper;
        }
        /// <returns></returns>
        [HttpGet, ProducesResponseType(typeof(IEnumerable<GetUserQueryResult>), StatusCodes.Status200OK), ProducesDefaultResponseType]
        public async Task<IActionResult> GetUsers()
        {
            var users = await Mediator.Send(new GetUsersQuery());
            return Ok(users);
        }
        [HttpGet("{userId}", Name = "GetUser"), ProducesResponseType(typeof(GetUserQueryResult), StatusCodes.Status200OK), ProducesDefaultResponseType]
        public async Task<IActionResult> GetUser(string userId)
        {
            var user = await Mediator.Send(new GetUserQuery { Id = userId });
            return Ok(user);
        }
        [HttpPost("CreateUser", Name = "CreateUser"), ProducesResponseType(typeof(GetUserQueryResult), StatusCodes.Status201Created), ProducesDefaultResponseType]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
        {
            var result = await Mediator.Send(command);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost(Name = "UpdateUser"), ProducesResponseType(typeof(GetUserQueryResult), StatusCodes.Status201Created),
         ProducesDefaultResponseType]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserCommand command)
        {
            var result = await Mediator.Send(command);
            var user = _mapper.Map<GetUserQueryResult>(result);
            return CreatedAtRoute("GetUser", new {userId = result.Id}, user);
        }

        [HttpDelete("{userId}", Name = "DeleteUser"),
         ProducesResponseType(typeof(DeleteUserCommandResult), StatusCodes.Status410Gone), ProducesDefaultResponseType]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var result = await Mediator.Send(new DeleteUserCommand{Id = userId});
            if (!result.IsDeleted)
            {
                return NotFound($"{result.Message}");
            }

            return NoContent();
        }
    }
}
