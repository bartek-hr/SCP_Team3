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
public sealed class SupplierHandlerTests : DatabaseTest
{
    protected override void SeedDatabase()
    {
        Context.Suppliers.Add(new Supplier
        {
            Id = 1,
            Code = "SUP-0001",
            Name = "Test Supplier",
            Address = "Street 1",
            City = "Amersfoort",
            ZipCode = "3811AA",
            Province = "Utrecht",
            Country = "Netherlands",
            ContactName = "Test Contact",
            PhoneNumber = "123456789",
            Reference = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task GetRoutesReturnJsonAndExpectedErrorsAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext list = await InvokeAsync(app, "/api/v1/suppliers");
        Assert.AreEqual(200, list.Response.StatusCode);
        using JsonDocument listJson = JsonDocument.Parse(await HttpContextTestHelpers.ReadResponseBodyAsync(list));
        Assert.AreEqual("SUP-0001", listJson.RootElement[0].GetProperty("code").GetString());
        Assert.AreEqual("123456789", listJson.RootElement[0].GetProperty("phone_number").GetString());

        DefaultHttpContext single = await InvokeAsync(app, "/api/v1/suppliers/{id}", "1");
        Assert.AreEqual(200, single.Response.StatusCode);
        using JsonDocument singleJson = JsonDocument.Parse(await HttpContextTestHelpers.ReadResponseBodyAsync(single));
        Assert.AreEqual("Test Supplier", singleJson.RootElement.GetProperty("name").GetString());

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "abc")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "999")).Response.StatusCode);
    }

    [TestMethod]
    public async Task ItemsRouteReturns501OnlyForExistingSupplierAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(501, (await InvokeAsync(app, "/api/v1/suppliers/{id}/items", "1")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/suppliers/{id}/items", "999")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}/items", "abc")).Response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUpdateDeleteAndMissingRecordsUseExpectedStatusesAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(new Supplier
        {
            Code = "SUP-0002",
            Name = "New Supplier",
            Address = "Street 2",
            City = "Amsterdam",
            ZipCode = "1000AA",
            Province = "Noord-Holland",
            Country = "Netherlands",
            ContactName = "New Contact",
            PhoneNumber = "987654321",
            Reference = "New"
        });

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/suppliers", body: body, method: "POST")).Response.StatusCode);
        Supplier created = Context.Suppliers.Single(supplier => supplier.Code == "SUP-0002");
        Assert.IsTrue(created.CreatedAt > DateTime.UnixEpoch);

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "1", body, "PUT")).Response.StatusCode);
        Assert.AreEqual("New Supplier", Context.Suppliers.Find(1)?.Name);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "999", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "abc", body, "PUT")).Response.StatusCode);

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "abc", method: "DELETE")).Response.StatusCode);
    }

    [TestMethod]
    public async Task InvalidRequestBodiesReturn400Async()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers", body: "{", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers", body: "[]", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "1", "{", "PUT")).Response.StatusCode);
    }

    private WebApplication CreateApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production,
        });
        builder.Services.AddSingleton<CargoHubDbContext>(Context);
        builder.Services.AddTransient<SupplierAccess>();
        builder.Services.AddTransient<SupplierLogic>();
        WebApplication app = builder.Build();
        app.MapDiscoveredRoutes(typeof(SupplierHandler).Assembly);
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
}
