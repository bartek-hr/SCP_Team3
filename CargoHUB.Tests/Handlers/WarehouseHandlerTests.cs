using System.Text.Json;
using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Handlers;

[TestClass]
public sealed class WarehouseHandlerTests : HandlerTest
{
    // Same shape as the legacy POST /warehouses request body.
    private const string LegacyPostBody = """
        {
          "id": 9001,
          "code": "TST-WH-01",
          "name": "Jumbo DC Test Warehouse",
          "address": "Teststraat 1",
          "city": "Rotterdam",
          "zip_code": "3011 AB",
          "province": "Zuid-Holland",
          "country": "Netherlands",
          "contact_name": "Test Persoon",
          "contact_phone": "(010) 1234567",
          "contact_email": "test-wh@jumbo-logistiek.nl"
        }
        """;

    protected override void SeedDatabase()
    {
        Context.Warehouses.AddRange(TestData.CreateWarehouse(1), TestData.CreateWarehouse(2, "Empty Warehouse"));
        Context.Locations.AddRange(TestData.CreateLocation(1, warehouseId: 1), TestData.CreateLocation(2, warehouseId: 1));
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task GetRoutesReturnJsonAndExpectedErrorsAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext list = await InvokeAsync(app, "/api/v1/warehouses");
        Assert.AreEqual(200, list.Response.StatusCode);
        using JsonDocument listJson = await ReadJsonAsync(list);
        Assert.AreEqual(2, listJson.RootElement.GetArrayLength());
        Assert.AreEqual("3011 AB", listJson.RootElement[0].GetProperty("zip_code").GetString());
        Assert.AreEqual("test-wh@example.com", listJson.RootElement[0].GetProperty("contact_email").GetString());

        DefaultHttpContext single = await InvokeAsync(app, "/api/v1/warehouses/{id}", "2");
        Assert.AreEqual(200, single.Response.StatusCode);
        using JsonDocument singleJson = await ReadJsonAsync(single);
        Assert.AreEqual("Empty Warehouse", singleJson.RootElement.GetProperty("name").GetString());

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "abc")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "0")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "999")).Response.StatusCode);
    }

    [TestMethod]
    public async Task LocationsRouteListsTheLocationsOfAWarehouseAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext locations = await InvokeAsync(app, "/api/v1/warehouses/{id}/locations", "1");
        Assert.AreEqual(200, locations.Response.StatusCode);
        using JsonDocument json = await ReadJsonAsync(locations);
        CollectionAssert.AreEqual(
            new[] { 1, 2 },
            json.RootElement.EnumerateArray().Select(location => location.GetProperty("id").GetInt32()).ToArray());
        Assert.AreEqual(1, json.RootElement[0].GetProperty("warehouse_id").GetInt32());

        DefaultHttpContext empty = await InvokeAsync(app, "/api/v1/warehouses/{id}/locations", "2");
        Assert.AreEqual(200, empty.Response.StatusCode);
        Assert.AreEqual("[]", await HttpContextTestHelpers.ReadResponseBodyAsync(empty));

        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/warehouses/{id}/locations", "999")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/warehouses/{id}/locations", "abc")).Response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUpdateAndDeleteUseExpectedStatusesAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/warehouses", body: LegacyPostBody, method: "POST")).Response.StatusCode);
        Warehouse created = Context.Warehouses.AsNoTracking().Single(warehouse => warehouse.Id == 9001);
        Assert.AreEqual("Jumbo DC Test Warehouse", created.Name);
        Assert.IsTrue(created.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/warehouses", body: LegacyPostBody, method: "POST")).Response.StatusCode);

        string body = JsonSerializer.Serialize(TestData.CreateWarehouse(name: "Renamed Warehouse"));
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "1", body, "PUT")).Response.StatusCode);
        Assert.AreEqual("Renamed Warehouse", Context.Warehouses.AsNoTracking().Single(warehouse => warehouse.Id == 1).Name);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "999", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "abc", body, "PUT")).Response.StatusCode);

        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "2", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "2", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "abc", method: "DELETE")).Response.StatusCode);
    }

    [TestMethod]
    public async Task InvalidRequestBodiesReturn400Async()
    {
        await using WebApplication app = CreateApplication();
        Warehouse invalidEmail = TestData.CreateWarehouse();
        invalidEmail.ContactEmail = "not-an-email";

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/warehouses", body: "{", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/warehouses", body: "[]", method: "POST")).Response.StatusCode);
        Assert.AreEqual(
            400,
            (await InvokeAsync(app, "/api/v1/warehouses", body: JsonSerializer.Serialize(invalidEmail), method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/warehouses/{id}", "1", "{", "PUT")).Response.StatusCode);
        Assert.AreEqual(2, Context.Warehouses.Count());
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<WarehouseAccess>()
        .AddTransient<LocationAccess>()
        .AddTransient<TransferAccess>()
        .AddTransient<WarehouseLogic>()
        .AddTransient<LocationLogic>());
}
