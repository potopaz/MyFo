using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFO.Infrastructure.Identity;

namespace MyFO.Infrastructure.Persistence;

/// <summary>
/// Seeds a SuperAdmin user on startup if none exists.
/// Credentials are read from configuration (SuperAdmin:Email / SuperAdmin:Password).
/// </summary>
public class SuperAdminSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SuperAdminSeeder> _logger;

    public SuperAdminSeeder(IServiceProvider services, IConfiguration configuration, ILogger<SuperAdminSeeder> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email    = _configuration["SuperAdmin:Email"];
        var password = _configuration["SuperAdmin:Password"];

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return;

        using var scope = _services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
            return;

        var user = new ApplicationUser
        {
            UserName      = email,
            Email         = email,
            FullName      = "Super Admin",
            IsSuperAdmin  = true,
            EmailConfirmed = true,
            CreatedAt     = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            _logger.LogInformation("SuperAdmin user created: {Email}", email);
        else
            _logger.LogError("Failed to create SuperAdmin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
