using MediatR;

namespace NextBus.Presentation.Users.Commands
{
     public class ResetPasswordCommand : IRequest<bool>
     {
          public string Username { get; set; }
          public string Token { get; set; }
          public string Password { get; set; }
     }
}