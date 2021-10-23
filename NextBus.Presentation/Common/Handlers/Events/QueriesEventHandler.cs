using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using NextBus.Domain.Events;

namespace NextBus.Presentation.Common.Handlers.Events
{
    public class QueriesEventHandler<T, E> : INotificationHandler<QueriesEvent<T, E>>
     {
          private readonly ILogger<QueriesEventHandler<T, E>> _logger;

          public QueriesEventHandler(ILogger<QueriesEventHandler<T, E>> logger)
          {
               _logger = logger;
          }

          public Task Handle(QueriesEvent<T, E> notification, CancellationToken cancellationToken)
          {
               var domainEvent = notification.DomainEvents;

               _logger.LogInformation($"{nameof(notification.Entities)} {notification.Query} Event: {domainEvent.GetType().Name} succeeded for {nameof(notification.Entities)} with Id: {notification.Id}");

               return Task.CompletedTask;
          }
     }
}