using MediatR;
using NextBus.Domain.Common;

namespace NextBus.Domain.Events
{
    public class QueryEvent<T, E> : Entity, INotification
     {
          public QueryEvent(E entity)
          {
               Entity = entity;
          }

          public E Entity { get; }
          public T Query { get; }
     }
}