using System;

namespace NextBus.Presentation.Users.Models.Result
{
     public class DeleteUserCommandResult
     {
          public string Id { get; set; }
          public bool IsDeleted { get; set; }
          public string Message { get; set; }
     }
}