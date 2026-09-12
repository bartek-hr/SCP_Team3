using CargoHUB.Datasource;
using CargoHUB.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// A container mounts this directory as its only writable persistent storage. The
// development default preserves the existing local SQLite location.
var dataDirectory = builder.Configuration["DataDirectory"]
                    ?? Path.Combine(builder.Environment.ContentRootPath, "Datasource");
Directory.CreateDirectory(dataDirectory);

var databasePath = Path.Combine(dataDirectory, "data.sqlite");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? $"Data Source={databasePath}";

builder.Services.AddDbContext<CargoHubDbContext>(options =>
    options.UseSqlite(connectionString));

// Production traffic can reach the app only through the local nginx proxy, which
// sets the original scheme and client IP. App containers have no published ports.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info = new()
        {
            Title = "CargoHUB WMS",
            Version = "1.0",
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors();

await app.Services.ApplyMigrationsAsync();

app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }))
    .ExcludeFromDescription();

app.MapDiscoveredRoutes();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");   // -> /openapi/v1.json
    app.MapScalarApiReference();                      // -> /scalar
}

await app.RunAsync();
