using MediatR;

namespace NextBus.Presentation.Users.Commands
{
     public class VerifyEmailCommand : IRequest<bool>
     {
          public string Username { get; set; }
          public string Token { get; set; }
     }
}