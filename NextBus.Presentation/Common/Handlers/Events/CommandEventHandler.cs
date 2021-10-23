using NextBus.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace NextBus.Presentation.Common.Handlers.Events
{
     public class CommandEventHandler<T, E> : INotificationHandler<CommandEvent<T, E>>
     {
          private readonly ILogger<CommandEventHandler<T, E>> _logger;

          public CommandEventHandler(ILogger<CommandEventHandler<T, E>> logger)
          {
               _logger = logger;
          }

          public Task Handle(CommandEvent<T, E> notification, CancellationToken cancellationToken)
          {
               var domainEvent = notification.DomainEvents;

               _logger.LogInformation($"{nameof(notification.Entity)} {notification.Command} Event: {domainEvent.GetType().Name} succeeded for {nameof(notification.Entity)} with Id: {notification.Id}");

               return Task.CompletedTask;
          }
     }
}