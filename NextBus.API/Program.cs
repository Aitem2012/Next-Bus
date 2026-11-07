using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NextBus.Application;
using NextBus.Infrastructure;
using NextBus.Persistence;
using NextBus.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

// Load local overrides (gitignored) and environment variables
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Ensure ContentRootPath is explicitly available for SQLite canonical path resolution
builder.Configuration["ContentRootPath"] = builder.Environment.ContentRootPath;

// Add Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT Bearer Authentication support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NextBus API",
        Version = "v1",
        Description = "NextBus - Ride-Sharing, Transit Booking, and Digital Wallet Platform (.NET 8)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Safely migrate database on startup preserving all existing records across restarts
await app.Services.MigrateDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NextBus API v1");
    c.RoutePrefix = string.Empty; // Swagger UI at root URL
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Redirect root to Swagger UI for seamless browser access
app.MapGet("/", () => Results.Redirect("/index.html"));

// Health check endpoint for uptime monitoring and PostgreSQL verification
app.MapGet("/health", async (AppDbContext dbContext) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new
        {
            status = "Healthy",
            service = "NextBus API (.NET 8)",
            database = "PostgreSQL (Connected)",
            timestamp = DateTime.UtcNow
        })
        : Results.Problem(detail: "Database connectivity check failed", statusCode: 503);
}).WithTags("Health");

await app.RunAsync();
