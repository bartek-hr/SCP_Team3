using System.Text.Json;
using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Handlers;

[TestClass]
public sealed class ShipmentHandlerTests : HandlerTest
{
    protected override void SeedDatabase()
    {
        SeedCatalogItems(1, 2);
        Context.Orders.Add(new Order { Id = 1 });
        Context.Shipments.Add(TestData.CreateShipment(1, orderId: 1, TestData.CreateShipmentItem(1, 2)));
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task PostStoresAShipmentForAnExistingOrderAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(TestData.CreateShipment(9001, orderId: 1, TestData.CreateShipmentItem(2, 1)));

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/shipments", body: body, method: "POST")).Response.StatusCode);
        Assert.AreEqual(1, Context.Shipments.Single(shipment => shipment.Id == 9001).OrderId);
    }

    [TestMethod]
    public async Task InvalidShipmentsReturnProblemDetailsAsync()
    {
        await using WebApplication app = CreateApplication();
        string unknownOrder = JsonSerializer.Serialize(TestData.CreateShipment(0, orderId: 999));

        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/shipments", body: unknownOrder, method: "POST"), 400, "order_id 999 does not exist.");
        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/shipments/{id}/orders", "1", "[999]", "PUT"), 400, "order_id 999 does not exist.");
        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/shipments/{id}/items", "1", """[{ "item_id": 2, "amount": 1 }, { "item_id": 2, "amount": 3 }]""", "PUT"),
            400,
            "item_id 2 appears more than once in items.");
        Assert.AreEqual(1, Context.Shipments.Count());
        Assert.AreEqual(1, Context.Shipments.Single().OrderId);
    }

    [TestMethod]
    public async Task ShipmentItemRoutesCheckReferencesAndDuplicatesAsync()
    {
        await using WebApplication app = CreateApplication();

        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/ShipmentItems", body: """{ "shipment_id": 999, "item_id": 2, "amount": 1 }""", method: "POST"),
            400,
            "shipment_id 999 does not exist.");
        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/ShipmentItems", body: """{ "shipment_id": 1, "item_id": 1, "amount": 1 }""", method: "POST"),
            409,
            "Shipment 1 already contains item_id 1.");
        Assert.AreEqual(1, Context.ShipmentItems.Count());
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<ShipmentDataAccess>()
        .AddTransient<ShipmentItemDataAccess>()
        .AddTransient<OrderDataAccess>()
        .AddTransient<ItemAccess>()
        .AddTransient<ShipmentLogic>()
        .AddTransient<ShipmentItemLogic>());
}
