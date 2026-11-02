using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Handlers;

[TestClass]
public sealed class InventoryHandlerTests : HandlerTest
{
    protected override void SeedDatabase()
    {
        SeedCatalogItems(1);
        Context.Warehouses.Add(TestData.CreateWarehouse(1));
        Context.Locations.Add(TestData.CreateLocation(1));
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task PostStoresInventoryForAnExistingItemAndLocationAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = """{ "item_id": 1, "location_id": 1, "quantity_on_hand": 5 }""";

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/inventories", body: body, method: "POST")).Response.StatusCode);
        Assert.AreEqual(5, Context.Inventories.Single().QuantityOnHand);
    }

    [DataTestMethod]
    [DataRow("""{ "item_id": 404, "location_id": 1 }""", "item_id 404 does not exist.")]
    [DataRow("""{ "item_id": 1, "location_id": 404 }""", "location_id 404 does not exist.")]
    [DataRow("""{ "location_id": 1 }""", "item_id 0 does not exist.")]
    [DataRow("""{ "item_id": 1, "location_id": 1, "quantity_allocated": -1 }""", "quantity_allocated cannot be negative.")]
    [DataRow("""{ "item_id": 1, "location_id": 1, "quantity_on_hand": "veel" }""", "The JSON request body is invalid at $.quantity_on_hand.")]
    public async Task InvalidInventoryReturnsProblemDetailsAsync(string body, string detail)
    {
        await using WebApplication app = CreateApplication();

        await AssertProblemAsync(await InvokeAsync(app, "/api/v1/inventories", body: body, method: "POST"), 400, detail);
        Assert.AreEqual(0, Context.Inventories.Count());
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<InventoryDataAccess>()
        .AddTransient<ItemAccess>()
        .AddTransient<LocationAccess>()
        .AddTransient<InventoryLogic>());
}
