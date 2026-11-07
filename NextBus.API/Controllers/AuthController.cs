using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextBus.Application.Common.Models;
using NextBus.Application.DTOs;
using NextBus.Application.Features.Auth;

namespace NextBus.API.Controllers
{
    public class AuthController : BaseApiController
    {
        [HttpPost("register")]
        [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await Mediator.Send(new RegisterUserCommand(dto));
            return HandleResult(result);
        }

        [HttpPost("verify-email-otp")]
        [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpDto dto)
        {
            var result = await Mediator.Send(new VerifyEmailOtpCommand(dto));
            return HandleResult(result);
        }

        [HttpPost("resend-email-otp")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendEmailOtp([FromQuery] string email)
        {
            var result = await Mediator.Send(new ResendEmailOtpCommand(email));
            return HandleResult(result);
        }

        [HttpPost("request-password-reset-otp")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestPasswordResetOtp([FromBody] RequestPasswordResetDto dto)
        {
            var result = await Mediator.Send(new RequestPasswordResetOtpCommand(dto));
            return HandleResult(result);
        }

        [HttpPost("reset-password-otp")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpDto dto)
        {
            var result = await Mediator.Send(new ResetPasswordWithOtpCommand(dto));
            return HandleResult(result);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<AuthResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await Mediator.Send(new LoginUserCommand(dto));
            return HandleResult(result);
        }

        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var result = await Mediator.Send(new GetCurrentUserQuery());
            return HandleResult(result);
        }

        [Authorize]
        [HttpPost("kyc")]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitKyc([FromBody] SubmitKycDto dto)
        {
            var result = await Mediator.Send(new SubmitUserKycCommand(dto));
            return HandleResult(result);
        }

        [Authorize]
        [HttpPost("driver-license")]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitDriverLicense([FromBody] SubmitDriverLicenseDto dto)
        {
            var result = await Mediator.Send(new SubmitDriverLicenseCommand(dto));
            return HandleResult(result);
        }
    }
}
