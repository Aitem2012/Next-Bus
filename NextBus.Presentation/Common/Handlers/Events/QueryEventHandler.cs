using NextBus.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Common.Handlers.Events
{
     public class QueryEventHandler<T, E> : INotificationHandler<QueryEvent<T, E>>
     {
          private readonly ILogger<QueryEventHandler<T, E>> _logger;

          public QueryEventHandler(ILogger<QueryEventHandler<T, E>> logger)
          {
               _logger = logger;
          }

          public Task Handle(QueryEvent<T, E> notification, CancellationToken cancellationToken)
          {
               var domainEvent = notification.DomainEvents;

               _logger.LogInformation($"{nameof(notification.Entity)} {notification.Query} Event: {domainEvent.GetType().Name} succeeded for {nameof(notification.Entity)} with Id: {notification.Id}");

               return Task.CompletedTask;
          }
     }
}