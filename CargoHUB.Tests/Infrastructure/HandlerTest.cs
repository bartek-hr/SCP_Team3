using System.Text;
using System.Text.Json;
using CargoHUB.Datasource;
using CargoHUB.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CargoHUB.Tests.Infrastructure;

public abstract class HandlerTest : DatabaseTest
{
    protected WebApplication CreateApplication(Action<IServiceCollection> registerServices)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production,
        });
        builder.Services.AddSingleton<CargoHubDbContext>(Context);
        registerServices(builder.Services);
        WebApplication app = builder.Build();
        app.MapDiscoveredRoutes();
        return app;
    }

    protected static async Task<DefaultHttpContext> InvokeAsync(
        WebApplication app, string route, string? id = null, string? body = null, string method = "GET")
    {
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == route
                                 && candidate.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains(method));
        DefaultHttpContext context = HttpContextTestHelpers.CreateContext(app.Services);
        context.Request.Method = method;
        context.Request.Path = route;
        if (id is not null)
        {
            context.Request.RouteValues["id"] = id;
        }

        if (body is not null)
        {
            context.Request.ContentType = "application/json";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        }

        await endpoint.RequestDelegate!(context);
        return context;
    }

    protected static async Task<JsonDocument> ReadJsonAsync(HttpContext context) =>
        JsonDocument.Parse(await HttpContextTestHelpers.ReadResponseBodyAsync(context));
}
