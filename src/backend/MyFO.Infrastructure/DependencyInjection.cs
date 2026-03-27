using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MyFO.Application.Auth;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Interfaces.Repositories;
using MyFO.Domain.Interfaces.Services;
using MyFO.Infrastructure.Auth;
using MyFO.Infrastructure.Email;
using MyFO.Infrastructure.Identity;
using MyFO.Infrastructure.Persistence;
using MyFO.Infrastructure.Persistence.Interceptors;
using MyFO.Infrastructure.Persistence.Repositories;
using MyFO.Infrastructure.Services;

namespace MyFO.Infrastructure;

/// <summary>
/// Extension method that registers all Infrastructure services in the DI container.
///
/// Called from Program.cs like: builder.Services.AddInfrastructure(builder.Configuration);
///
/// This is where we wire up:
///   - PostgreSQL connection (via EF Core)
///   - ASP.NET Identity (user management, passwords, etc.)
///   - JWT authentication
///   - Our interceptors (audit fields, domain events)
///   - Our services (CurrentUserService, Repository, AuthService)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- EF Core + PostgreSQL ---
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<TenantConnectionInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
            var domainEventDispatcher = sp.GetRequiredService<DomainEventDispatcher>();
            var tenantInterceptor = sp.GetRequiredService<TenantConnectionInterceptor>();

            // Use AppConnection (myfo_app role) for RLS enforcement.
            // Falls back to DefaultConnection if AppConnection is not configured.
            var connectionString = configuration.GetConnectionString("AppConnection")
                                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString)
                   .AddInterceptors(tenantInterceptor, domainEventDispatcher, auditInterceptor);
        });

        // --- ASP.NET Identity ---
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // --- JWT Authentication ---
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        // --- External Auth Settings ---
        var externalAuth = configuration.GetSection("ExternalAuth").Get<ExternalAuthSettings>() ?? new ExternalAuthSettings();
        services.Configure<ExternalAuthSettings>(configuration.GetSection("ExternalAuth"));

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
        })
        .AddCookie("ExternalCookie", options =>
        {
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });

        // Register external OAuth providers if configured
        if (externalAuth.Google.Enabled)
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = externalAuth.Google.ClientId;
                options.ClientSecret = externalAuth.Google.ClientSecret;
                options.SignInScheme = "ExternalCookie";
            });
        }

        if (externalAuth.Microsoft.Enabled)
        {
            authBuilder.AddMicrosoftAccount(options =>
            {
                options.ClientId = externalAuth.Microsoft.ClientId;
                options.ClientSecret = externalAuth.Microsoft.ClientSecret;
                options.SignInScheme = "ExternalCookie";
            });
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy("SuperAdmin", policy =>
                policy.RequireClaim("is_super_admin", "true"));
        });

        // --- Email ---
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.AddHttpClient("Resend", client =>
        {
            var apiKey = configuration["Email:ResendApiKey"] ?? string.Empty;
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        });
        services.AddScoped<IEmailService, EmailService>();

        // --- Application services ---
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Admin DbContext: uses DefaultConnection (postgres superuser) — bypasses RLS
        services.AddDbContext<AdminDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? configuration.GetConnectionString("AppConnection");
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IAdminDbContext>(sp => sp.GetRequiredService<AdminDbContext>());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IVerificationTokenService, VerificationTokenService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddHttpContextAccessor();

        // --- Exchange rate service ---
        services.AddHttpClient<IExchangeRateService, ExchangeRateService>();

        // --- SuperAdmin seeder ---
        services.AddHostedService<SuperAdminSeeder>();

        return services;
    }
}
