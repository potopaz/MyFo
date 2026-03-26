using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MyFO.Application;

/// <summary>
/// Extension method that registers all Application layer services.
///
/// Called from Program.cs like: builder.Services.AddApplication();
///
/// Registers:
///   - MediatR handlers (commands, queries, domain event handlers)
///   - FluentValidation validators
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register all MediatR handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
