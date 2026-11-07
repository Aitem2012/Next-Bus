using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using NextBus.Application.Common.Models;

namespace NextBus.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .Select(f => f.ErrorMessage)
                .Distinct()
                .ToList();

            if (failures.Count != 0)
            {
                // If response is Result<T>
                if (typeof(TResponse).IsGenericType &&
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(IEnumerable<string>)]);
                    if (failureMethod != null)
                    {
                        return (TResponse)failureMethod.Invoke(null, [failures])!;
                    }
                }

                // If response is Result
                if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure(failures);
                }
            }

            return await next();
        }
    }
}

