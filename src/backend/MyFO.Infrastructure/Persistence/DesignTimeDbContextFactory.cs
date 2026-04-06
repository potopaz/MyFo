using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Infrastructure.Persistence;

/// <summary>
/// Factory used ONLY by EF Core tooling (migrations, scaffolding) at design time.
///
/// When you run "dotnet ef migrations add ...", EF Core needs to create an instance
/// of ApplicationDbContext without the full application running. This factory
/// provides a minimal setup: just the connection string and a dummy user service.
///
/// This class is NEVER used at runtime — the real DI container handles that.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "MyFO.API"))
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }

    /// <summary>
    /// Dummy implementation for design time. Returns empty values since
    /// migrations don't need a real user context.
    /// </summary>
    private class DesignTimeCurrentUserService : ICurrentUserService
    {
        public Guid UserId => Guid.Empty;
        public Guid? FamilyId => null;
        public bool IsSuperAdmin => false;
        public bool IsFamilyAdmin => false;
    }
}
