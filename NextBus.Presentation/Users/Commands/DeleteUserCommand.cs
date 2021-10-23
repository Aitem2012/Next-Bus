using MediatR;
using NextBus.Presentation.Users.Models.Result;

namespace NextBus.Presentation.Users.Commands
{
    public class DeleteUserCommand : IRequest<DeleteUserCommandResult>
     {
          public string Id { get; set; }
     }
}