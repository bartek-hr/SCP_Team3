using CargoHUB.Datasource;
using CargoHUB.Framework;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Datasource");
Directory.CreateDirectory(dataDirectory);

var databasePath = Path.Combine(dataDirectory, "data.sqlite");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? $"Data Source={databasePath}";

builder.Services.AddDbContext<CargoHubDbContext>(options =>
    options.UseSqlite(connectionString));

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

app.UseCors();

await app.Services.ApplyMigrationsAsync();

app.MapDiscoveredRoutes();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");   // -> /openapi/v1.json
    app.MapScalarApiReference();                      // -> /scalar
}

await app.RunAsync();
