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
public sealed class OrderHandlerTests : HandlerTest
{
    protected override void SeedDatabase()
    {
        SeedCatalogItems(1, 2);
        Context.Clients.Add(TestData.CreateClient(1));
        Context.Warehouses.Add(TestData.CreateWarehouse(1));
        Context.Orders.AddRange(TestData.CreateOrder(1, items: [TestData.CreateOrderItem(1, 2)]), TestData.CreateOrder(2));
        Context.Shipments.Add(TestData.CreateShipment(1, orderId: 1));
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task PostStoresAnOrderWithExistingReferencesAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(TestData.CreateOrder(9001, items: [TestData.CreateOrderItem(1, 3)]));

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/orders", body: body, method: "POST")).Response.StatusCode);
        Assert.AreEqual(1, Context.OrderItems.Count(item => item.OrderId == 9001));
    }

    [TestMethod]
    public async Task InvalidOrdersReturnProblemDetailsAsync()
    {
        await using WebApplication app = CreateApplication();
        Order unknownClient = TestData.CreateOrder();
        unknownClient.ClientId = 999;
        Order requestedTooEarly = TestData.CreateOrder();
        requestedTooEarly.RequestDate = requestedTooEarly.OrderDate.AddDays(-1);
        Order unknownItem = TestData.CreateOrder(items: [TestData.CreateOrderItem(404, 1)]);

        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/orders", body: JsonSerializer.Serialize(unknownClient), method: "POST"),
            400,
            "client_id 999 does not exist.");
        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/orders", body: JsonSerializer.Serialize(requestedTooEarly), method: "POST"),
            400,
            "request_date cannot be before order_date.");
        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/orders/{id}", "2", JsonSerializer.Serialize(unknownItem), "PUT"),
            400,
            "items[0].item_id 404 does not exist.");
        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/orders/{id}/items", "1", """[{ "item_id": 1, "amount": 0 }]""", "PUT"),
            400,
            "items[0].amount must be greater than 0.");
        Assert.AreEqual(2, Context.Orders.Count());
        Assert.AreEqual(2, Context.OrderItems.Single().Amount);
    }

    [TestMethod]
    public async Task DeletingAnOrderWithShipmentsReturns409Async()
    {
        await using WebApplication app = CreateApplication();

        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/orders/{id}", "1", method: "DELETE"), 409, "Order 1 still has shipments.");
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/orders/{id}", "2", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(1, Context.Orders.Count());
    }

    [TestMethod]
    public async Task OrderItemRoutesCheckAmountsReferencesAndDuplicatesAsync()
    {
        await using WebApplication app = CreateApplication();

        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/orderitems", body: """{ "order_id": 1, "item_id": 2, "amount": 0 }""", method: "POST"),
            400,
            "amount must be greater than 0.");
        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/orderitems", body: """{ "order_id": 999, "item_id": 2, "amount": 1 }""", method: "POST"),
            400,
            "order_id 999 does not exist.");
        await AssertProblemAsync(
            await InvokeAsync(app, "/api/v1/orderitems", body: """{ "order_id": 1, "item_id": 1, "amount": 5 }""", method: "POST"),
            409,
            "Order 1 already contains item_id 1.");
        Assert.AreEqual(
            201,
            (await InvokeAsync(app, "/api/v1/orderitems", body: """{ "order_id": 1, "item_id": 2, "amount": 5 }""", method: "POST")).Response.StatusCode);
        Assert.AreEqual(2, Context.OrderItems.Count(item => item.OrderId == 1));
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<OrderDataAccess>()
        .AddTransient<OrderItemDataAccess>()
        .AddTransient<ClientAccess>()
        .AddTransient<WarehouseAccess>()
        .AddTransient<ItemAccess>()
        .AddTransient<ShipmentDataAccess>()
        .AddTransient<OrderLogic>()
        .AddTransient<OrderItemLogic>());
}
