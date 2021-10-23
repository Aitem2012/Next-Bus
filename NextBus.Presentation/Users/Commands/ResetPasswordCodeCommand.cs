using MediatR;

namespace NextBus.Presentation.Users.Commands
{
     public class ResetPasswordCodeCommand : IRequest<string>
     {
          public string Username { get; set; }
     }
}