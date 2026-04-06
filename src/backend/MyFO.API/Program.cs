using MyFO.Application;
using MyFO.Infrastructure;
using MyFO.Infrastructure.Persistence;
using MyFO.API.Middleware;
using MyFO.Domain.Interfaces.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Trust the reverse proxy (Railway, Vercel, etc.) for X-Forwarded-* headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// --- Register services from each layer ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- CORS ---
var frontendUrl = builder.Configuration["App:FrontendUrl"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- API services ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Auto-apply pending migrations on startup ---
// Uses DefaultConnection (postgres superuser) for DDL — myfo_app lacks DDL permissions.
// This temporary context is only used to run migrations; runtime uses the registered ApplicationDbContext with RLS.
using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection")
                        ?? configuration.GetConnectionString("AppConnection")!;
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
    using var db = new ApplicationDbContext(options, currentUser);
    db.Database.Migrate();
}

// --- Middleware pipeline ---
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Not needed for local HTTP-only development
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
