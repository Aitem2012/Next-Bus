using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NextBus.Application.Common.Behaviors;

namespace NextBus.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddAutoMapper(assembly);
            services.AddValidatorsFromAssembly(assembly);
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            return services;
        }
    }
}
