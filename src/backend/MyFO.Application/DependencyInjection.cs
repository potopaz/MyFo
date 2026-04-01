using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MyFO.Application.Common.Mediator;

namespace MyFO.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register IMediator
        services.AddScoped<IMediator, Mediator>();

        // Scan and register all handlers from this assembly
        var twoParam = typeof(IRequestHandler<,>);
        var oneParam = typeof(IRequestHandler<>);
        var notifParam = typeof(INotificationHandler<>);

        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                var def = iface.GetGenericTypeDefinition();
                if (def == twoParam || def == oneParam || def == notifParam)
                {
                    services.AddScoped(iface, type);
                }
            }
        }

        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
