using System.Text.Json;
using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Handlers;

[TestClass]
public sealed class LocationHandlerTests : HandlerTest
{
    // Same shape as the legacy POST /locations request body.
    private const string LegacyPostBody = """
        {
          "id": 9001,
          "warehouse_id": 1,
          "code": "VGH-AMB-B99-R1-B1",
          "name": "Zone B Aisle 99 Rack 1 Bin 1"
        }
        """;

    protected override void SeedDatabase()
    {
        Context.Warehouses.AddRange(TestData.CreateWarehouse(1), TestData.CreateWarehouse(2));
        Context.Locations.AddRange(
            TestData.CreateLocation(1),
            TestData.CreateLocation(2),
            TestData.CreateLocation(3, warehouseId: 2));
        Context.Transfers.Add(TestData.CreateTransfer(1, 1, 2));
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task GetRoutesReturnJsonAndExpectedErrorsAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext list = await InvokeAsync(app, "/api/v1/locations");
        Assert.AreEqual(200, list.Response.StatusCode);
        using JsonDocument listJson = await ReadJsonAsync(list);
        Assert.AreEqual(3, listJson.RootElement.GetArrayLength());
        Assert.AreEqual(2, listJson.RootElement[2].GetProperty("warehouse_id").GetInt32());

        DefaultHttpContext single = await InvokeAsync(app, "/api/v1/locations/{id}", "1");
        Assert.AreEqual(200, single.Response.StatusCode);
        using JsonDocument singleJson = await ReadJsonAsync(single);
        Assert.AreEqual("TST-A01-R1-B1", singleJson.RootElement.GetProperty("code").GetString());

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/locations/{id}", "abc")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/locations/{id}", "999")).Response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUpdateAndDeleteUseExpectedStatusesAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/locations", body: LegacyPostBody, method: "POST")).Response.StatusCode);
        Assert.AreEqual("VGH-AMB-B99-R1-B1", Context.Locations.AsNoTracking().Single(location => location.Id == 9001).Code);
        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/locations", body: LegacyPostBody, method: "POST")).Response.StatusCode);

        string moveBody = JsonSerializer.Serialize(TestData.CreateLocation(warehouseId: 2, name: "Moved Location"));
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/locations/{id}", "9001", moveBody, "PUT")).Response.StatusCode);
        Assert.AreEqual(2, Context.Locations.AsNoTracking().Single(location => location.Id == 9001).WarehouseId);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/locations/{id}", "999", moveBody, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/locations/{id}", "abc", moveBody, "PUT")).Response.StatusCode);

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/locations/{id}", "9001", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/locations/{id}", "9001", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/locations/{id}", "abc", method: "DELETE")).Response.StatusCode);
    }

    [TestMethod]
    public async Task DeletingALocationThatTransfersUseReturns409Async()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/locations/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/locations/{id}", "2", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(3, Context.Locations.Count());
    }

    [TestMethod]
    public async Task UnknownWarehouseAndInvalidBodiesReturn400Async()
    {
        await using WebApplication app = CreateApplication();
        string unknownWarehouse = JsonSerializer.Serialize(TestData.CreateLocation(warehouseId: 999));

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/locations", body: unknownWarehouse, method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/locations/{id}", "1", unknownWarehouse, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/locations", body: "{", method: "POST")).Response.StatusCode);
        Assert.AreEqual(3, Context.Locations.Count());
        Assert.AreEqual(1, Context.Locations.AsNoTracking().Single(location => location.Id == 1).WarehouseId);
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<LocationAccess>()
        .AddTransient<WarehouseAccess>()
        .AddTransient<TransferAccess>()
        .AddTransient<LocationLogic>());
}
