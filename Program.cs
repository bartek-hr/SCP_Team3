using CargoHUB.Framework;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

app.MapDiscoveredRoutes();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");   // -> /openapi/v1.json
    app.MapScalarApiReference();                      // -> /scalar
}

await app.RunAsync();
