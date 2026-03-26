using MyFO.Application;
using MyFO.Infrastructure;
using MyFO.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// --- Register services from each layer ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- API services ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Middleware pipeline ---
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Not needed for local HTTP-only development
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
