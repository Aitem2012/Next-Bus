using System;

namespace NextBus.Presentation.Common.Models.Results
{
     public class DeleteCommandResult
     {
          public Guid Id { get; set; }
          public bool IsDeleted { get; set; }
          public string Message { get; set; }
     }
}