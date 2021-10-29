using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextBus.API.Security;
using NextBus.Common.Settings;
using NextBus.Common.Utilities;
using NextBus.Data.Data;
using NextBus.Domain.Users;

namespace NextBus.API.Controllers
{
    [Route("api/v1/[Controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IOptions<JWTData> _JWTData;
        
        public AuthController(ILogger<AuthController> logger, UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager, IOptions<JWTData> options)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _JWTData = options;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                var msg = Utilities.CreateResponse(
                                message: "Incomplete model", errs: ModelState, data: "");
                return BadRequest(msg);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Invalid Credential");
                var errMsg = Utilities.CreateResponse(message: "Invalid Credentials", errs: ModelState, data: "");
                return BadRequest(errMsg);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("Email", "Email not confirmed yet");
                var errMsg = Utilities.CreateResponse(message: "Email not confirmed", errs: ModelState, data: "");
                return BadRequest(errMsg);
            }

            var checkPassword = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (!checkPassword.Succeeded)
            {
                ModelState.AddModelError("Password", "Invalid Credential");
                var errMsg = Utilities.CreateResponse(message: "Invalid Credentials", errs: ModelState, data: "");
                return BadRequest(errMsg);
            }

            var userRoles = await _userManager.GetRolesAsync(user) as List<string>;
            var token = JWTService.GenerateToken(user, userRoles, _JWTData);

            LoginResponse res = new LoginResponse
            {
                Token = token,
                Role = string.Join(",", await _userManager.GetRolesAsync(user)),
                UserId = user.Id
            };

            return Ok(Utilities.CreateResponse("Login Successful", null, res));
        }


       
        //base_url/api/v1/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                var responseObj = Utilities.CreateResponse<string>("Model errors", ModelState, "");
                return BadRequest(responseObj);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Email does not exist");
                var responseObj = Utilities.CreateResponse<string>($"Invalid Email", ModelState, "");

                return BadRequest(responseObj);
            }

            //Get the password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var queryParams = new Dictionary<string, string>()
            {
                ["email"] = user.Email,
                ["token"] = token
            };
            //var template = NotificationsHelper.EmailHtmlStringTemplate($"{user.FirstName} {user.LastName}", "account/reset-password", queryParams, "resetpasswordemailtemplate.html", HttpContext);

            ////send password reset email to user
            //var emailSent = await _notificationService.SendEmailAsync(user.Email, "Reset Password", template, "");
            //if (emailSent)
            //{
            //    var responseObj = Utilities.CreateResponse<string>($"Successfully send forgot password mail", null, "");
            //    return Ok(responseObj);
            //}
            //else
            //{
            //    ModelState.AddModelError("EmailService", "There was an error sending the password reset link. Please try again");
            //    return BadRequest(Utilities.CreateResponse<string>("Service not available", ModelState, ""));
            //}
            return Ok(queryParams);
        }

        //base_url/api/v1/auth/reset-password
        [HttpPatch("reset-password")]
        public async Task<ActionResult<Response<string>>> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                var responseObj = Utilities.CreateResponse<string>("Model errors", ModelState, null);
                return BadRequest(responseObj);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Email does not exist");
                var responseObj = Utilities.CreateResponse<string>("Model errors", ModelState, null);
                return BadRequest(responseObj);
            }

            //var token = HttpUtility.UrlDecode(model.Token);
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return Ok(Utilities.CreateResponse<string>("Password reset was successful", null, null));
            }
            else
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }
                return BadRequest(Utilities.CreateResponse<string>("Token", ModelState, null));
            }
        }

        

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            return Ok();
        }

        [HttpPatch("change-password/{id}")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model, [FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                ModelState.AddModelError("Id", "Id must be provided");
                return BadRequest(Utilities.CreateResponse("Id not provided", ModelState, ""));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(Utilities.CreateResponse("Model state error", ModelState, ""));
            }

            var currentLoggedInUser = await _userManager.GetUserAsync(User);


            if (!await _userManager.IsInRoleAsync(currentLoggedInUser, "Admin"))
            {
                if (currentLoggedInUser.Id != id)
                {
                    ModelState.AddModelError("Id", "could not authorize user");
                    return Unauthorized(Utilities.CreateResponse("Id is invalid", ModelState, ""));
                }
            }

            var user = await _userManager.FindByIdAsync(id);
            var passwordExist = await _userManager.CheckPasswordAsync(user, model.OldPassword);

            if (!passwordExist)
            {
                ModelState.AddModelError("Password", "Invalid password provided");
                return BadRequest(Utilities.CreateResponse("Invalid Credential", ModelState, ""));
            }

            var res = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (!res.Succeeded)
            {
                foreach (var err in res.Errors)
                {
                    ModelState.AddModelError(err.Code, err.Description);
                }

                return BadRequest(Utilities.CreateResponse("Password", ModelState, ""));
            }

            return Ok(Utilities.CreateResponse("Password changed successfully", null, ""));
        }
        
    }
}
