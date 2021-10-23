using System.Collections.Generic;

namespace NextBus.Domain.Common
{
     public interface IHasDomainEvent
     {
          public List<DomainEvent> DomainEvents { get; set; }
     }
}