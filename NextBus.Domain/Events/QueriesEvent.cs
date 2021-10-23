using NextBus.Domain.Common;
using MediatR;
using System.Collections.Generic;

namespace NextBus.Domain.Events
{
     public class QueriesEvent<T, E> : Entity, INotification
     {
          public QueriesEvent(List<E> entities)
          {
               Entities = entities;
          }

          public List<E> Entities { get; }
          public T Query { get; }
     }
}