using NextBus.Domain.Common;
using MediatR;

namespace NextBus.Domain.Events
{
     public class CommandEvent<T, E> : Entity, INotification
     {
          public CommandEvent(E entity)
          {
               Entity = entity;
          }

          public E Entity { get; }
          public T Command { get; }
     }
}