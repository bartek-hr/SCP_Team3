using System.Text;
using System.Text.Json;
using CargoHUB.Access;
using CargoHUB.Datasource;
using CargoHUB.Framework;
using CargoHUB.Handlers;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Handlers;

[TestClass]
public sealed class ItemTypeHandlerTests : DatabaseTest
{
    protected override void SeedDatabase()
    {
        Context.ItemTypes.Add(CreateItemType(1));
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task GetRoutesReturnJsonAndExpectedStatusesAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext list = await InvokeAsync(app, "/api/v1/item_types");
        Assert.AreEqual(200, list.Response.StatusCode);
        using JsonDocument listJson = JsonDocument.Parse(await HttpContextTestHelpers.ReadResponseBodyAsync(list));
        Assert.AreEqual("Single", listJson.RootElement[0].GetProperty("name").GetString());
        Assert.AreEqual("Packaging tier: Single", listJson.RootElement[0].GetProperty("description").GetString());

        DefaultHttpContext single = await InvokeAsync(app, "/api/v1/item_types/{id}", "1");
        Assert.AreEqual(200, single.Response.StatusCode);
        using JsonDocument singleJson = JsonDocument.Parse(await HttpContextTestHelpers.ReadResponseBodyAsync(single));
        Assert.IsTrue(singleJson.RootElement.TryGetProperty("created_at", out _));
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}", "abc")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_types/{id}", "999")).Response.StatusCode);
    }

    [TestMethod]
    public async Task ItemsRouteReturns501OnlyForExistingItemTypeAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(501, (await InvokeAsync(app, "/api/v1/item_types/{id}/items", "1")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_types/{id}/items", "999")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}/items", "abc")).Response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUpdateDeleteAndMissingRecordsUseExpectedStatusesAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(CreateItemType());

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/item_types", body: body, method: "POST")).Response.StatusCode);
        Assert.IsTrue(Context.ItemTypes.Any(itemType => itemType.Id != 1));

        ItemType updated = CreateItemType(1);
        updated.Name = "Updated type";
        string updatedBody = JsonSerializer.Serialize(updated);
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/item_types/{id}", "1", updatedBody, "PUT")).Response.StatusCode);
        Assert.AreEqual("Updated type", Context.ItemTypes.Find(1)?.Name);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_types/{id}", "999", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}", "abc", body, "PUT")).Response.StatusCode);

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/item_types/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_types/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}", "abc", method: "DELETE")).Response.StatusCode);
    }

    [TestMethod]
    public async Task InvalidRequestBodiesReturn400Async()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types", body: "{", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types", body: "[]", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}", "1", "{", "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types", body: "{\"name\":\"No description\"}", method: "POST")).Response.StatusCode);
    }

    private WebApplication CreateApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production,
        });
        builder.Services.AddSingleton<CargoHubDbContext>(Context);
        builder.Services.AddTransient<ItemTypeAccess>();
        builder.Services.AddTransient<ItemTypeLogic>();
        WebApplication app = builder.Build();
        app.MapDiscoveredRoutes(typeof(ItemTypeHandler).Assembly);
        return app;
    }

    private static async Task<DefaultHttpContext> InvokeAsync(
        WebApplication app, string route, string? id = null, string? body = null, string method = "GET")
    {
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == route
                                 && candidate.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains(method));
        DefaultHttpContext context = HttpContextTestHelpers.CreateContext(app.Services);
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

    private static ItemType CreateItemType(int id = 0) => new()
    {
        Id = id,
        Name = "Single",
        Description = "Packaging tier: Single",
    };
}
