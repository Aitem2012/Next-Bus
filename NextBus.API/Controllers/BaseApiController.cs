using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NextBus.Application.Common.Models;

namespace NextBus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        private IMediator? _mediator;
        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

        protected ActionResult HandleResult<T>(Result<T> result)
        {
            if (result == null)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        protected ActionResult HandleResult(Result result)
        {
            if (result == null)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}

