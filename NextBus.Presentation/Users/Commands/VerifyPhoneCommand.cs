using MediatR;

namespace NextBus.Presentation.Users.Commands
{
     public class VerifyPhoneCommand : IRequest<bool>
     {
          public string Username { get; set; }
          public string Token { get; set; }
     }
}