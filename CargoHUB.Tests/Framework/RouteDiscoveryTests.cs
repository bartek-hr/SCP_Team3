using System.Text.Json;
using CargoHUB.Handlers;
using CargoHUB.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Framework;

[TestClass]
public sealed class RouteDiscoveryTests
{
    [TestMethod]
    public async Task MapsDiscoveredRoutesWithHttpMethodMetadataAndInvokesHandlersAsync()
    {
        RouteInvocationLog.Reset();
        await using var app = CreateApplication();
        var endpoint = FindEndpoint(app, "/discovered/{user}");
        var metadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
        var context = HttpContextTestHelpers.CreateContext(app.Services);
        context.Request.Path = "/discovered/Ada";
        context.Request.RouteValues["user"] = "Ada";

        Assert.IsNotNull(metadata);
        CollectionAssert.Contains(metadata!.HttpMethods.ToArray(), HttpMethods.Get);

        await endpoint.RequestDelegate!(context);

        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.AreEqual(
            "Hello Ada!",
            JsonSerializer.Deserialize<string>(await HttpContextTestHelpers.ReadResponseBodyAsync(context)));
    }

    [TestMethod]
    public async Task StopsBeforeMiddlewareWhenItReturnsAResponseAsync()
    {
        RouteInvocationLog.Reset();
        await using var app = CreateApplication();
        var endpoint = FindEndpoint(app, "/discovered/blocked");
        var context = HttpContextTestHelpers.CreateContext(app.Services);

        await endpoint.RequestDelegate!(context);

        Assert.AreEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        CollectionAssert.AreEqual(new[] { "block" }, RouteInvocationLog.Events.ToArray());
        Assert.AreEqual(string.Empty, await HttpContextTestHelpers.ReadResponseBodyAsync(context));
    }

    [TestMethod]
    public async Task RunsBeforeAndAfterMiddlewareAroundTheRouteAsync()
    {
        RouteInvocationLog.Reset();
        await using var app = CreateApplication();
        var endpoint = FindEndpoint(app, "/discovered/hooks");
        var context = HttpContextTestHelpers.CreateContext(app.Services);

        await endpoint.RequestDelegate!(context);

        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
        CollectionAssert.AreEqual(
            new[] { "before", "route", "after" },
            RouteInvocationLog.Events.ToArray());
        Assert.AreEqual(
            "completed",
            JsonSerializer.Deserialize<string>(await HttpContextTestHelpers.ReadResponseBodyAsync(context)));
    }

    [TestMethod]
    public async Task AllowsAfterMiddlewareToReplaceTheRouteResponseAsync()
    {
        RouteInvocationLog.Reset();
        await using var app = CreateApplication();
        var endpoint = FindEndpoint(app, "/discovered/override");
        var context = HttpContextTestHelpers.CreateContext(app.Services);

        await endpoint.RequestDelegate!(context);

        Assert.AreEqual(StatusCodes.Status409Conflict, context.Response.StatusCode);
        CollectionAssert.AreEqual(
            new[] { "override-route", "override-after" },
            RouteInvocationLog.Events.ToArray());
        Assert.AreEqual(string.Empty, await HttpContextTestHelpers.ReadResponseBodyAsync(context));
    }

    private static WebApplication CreateApplication()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production,
        });
        var app = builder.Build();
        app.MapDiscoveredRoutes(typeof(RouteDiscoveryTestHandler).Assembly);
        return app;
    }

    private static RouteEndpoint FindEndpoint(WebApplication app, string route)
    {
        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>();

        return endpoints.Single(endpoint => endpoint.RoutePattern.RawText == route);
    }
}

internal static class RouteInvocationLog
{
    private static readonly List<string> Calls = [];

    public static IReadOnlyList<string> Events => Calls;

    public static void Add(string value) => Calls.Add(value);

    public static void Reset() => Calls.Clear();
}
