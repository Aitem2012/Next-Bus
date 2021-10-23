using MediatR;

namespace NextBus.Presentation.Users.Commands
{
     public class VerifyUserCommand : IRequest<bool>
     {
          public string Username { get; set; }
     }
}