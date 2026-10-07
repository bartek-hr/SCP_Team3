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
public sealed class ClientHandlerTests : HandlerTest
{
    protected override void SeedDatabase()
    {
        Context.Clients.Add(CreateClient(1));
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task GetRoutesReturnClientJsonAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext list = await InvokeAsync(app, "/api/v1/clients");
        Assert.AreEqual(200, list.Response.StatusCode);
        using JsonDocument listJson = await ReadJsonAsync(list);
        Assert.AreEqual(1, listJson.RootElement.GetArrayLength());
        Assert.AreEqual("test@example.com", listJson.RootElement[0].GetProperty("contact_email").GetString());
        Assert.AreEqual("3811AA", listJson.RootElement[0].GetProperty("zip_code").GetString());

        DefaultHttpContext single = await InvokeAsync(app, "/api/v1/clients/{id}", "1");
        Assert.AreEqual(200, single.Response.StatusCode);
        using JsonDocument singleJson = await ReadJsonAsync(single);
        Assert.AreEqual("Test Client", singleJson.RootElement.GetProperty("name").GetString());
        Assert.AreEqual(TestData.CreatedAt, singleJson.RootElement.GetProperty("created_at").GetDateTime());
    }

    [DataTestMethod]
    [DataRow("abc")]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("2147483648")]
    public async Task InvalidIdsReturn400Async(string id)
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(CreateClient());

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/clients/{id}", id)).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/clients/{id}/orders", id)).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/clients/{id}", id, body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/clients/{id}", id, method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(1, Context.Clients.Count());
    }

    [TestMethod]
    public async Task MissingClientsReturn404ForSingleRecordRoutesAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(CreateClient());

        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/clients/{id}", "999")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/clients/{id}", "999", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/clients/{id}", "999", method: "DELETE")).Response.StatusCode);
    }

    [TestMethod]
    public async Task OrdersRouteFiltersByClientShippingAndBillingAndIncludesItemsAsync()
    {
        Context.Orders.AddRange(
            CreateOrder(4, clientId: 2),
            CreateOrder(3, billToClientId: 1),
            CreateOrder(2, shipToClientId: 1),
            CreateOrder(1, clientId: 1, shipToClientId: 1, billToClientId: 1));
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        await using WebApplication app = CreateApplication();

        DefaultHttpContext response = await InvokeAsync(app, "/api/v1/clients/{id}/orders", "1");

        Assert.AreEqual(200, response.Response.StatusCode);
        using JsonDocument json = await ReadJsonAsync(response);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, json.RootElement.EnumerateArray()
            .Select(order => order.GetProperty("id").GetInt32()).ToArray());
        JsonElement item = json.RootElement[0].GetProperty("items")[0];
        Assert.AreEqual(10, item.GetProperty("item_id").GetInt32());
        Assert.AreEqual(5, item.GetProperty("amount").GetInt32());
        Assert.AreEqual(2.50m, item.GetProperty("unit_price").GetDecimal());
    }

    [TestMethod]
    public async Task OrdersRouteReturnsEmptyArrayWhenNoOrdersMatchAsync()
    {
        await using WebApplication app = CreateApplication();

        foreach (string id in new[] { "1", "999" })
        {
            DefaultHttpContext response = await InvokeAsync(app, "/api/v1/clients/{id}/orders", id);
            Assert.AreEqual(200, response.Response.StatusCode);
            Assert.AreEqual("[]", await HttpContextTestHelpers.ReadResponseBodyAsync(response));
        }
    }

    [TestMethod]
    public async Task CreateUpdateDeletePersistChangesAndProtectIdentityAndTimestampsAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(CreateClient(9001));

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/clients", body: body, method: "POST")).Response.StatusCode);
        Client created = Context.Clients.AsNoTracking().Single(client => client.Id == 9001);
        Assert.IsTrue(created.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(created.CreatedAt, created.UpdatedAt);

        Client updated = CreateClient(9002);
        updated.Name = "Updated Client";
        updated.CreatedAt = DateTime.UtcNow.AddYears(1);
        updated.UpdatedAt = updated.CreatedAt;
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/clients/{id}", "1",
            JsonSerializer.Serialize(updated), "PUT")).Response.StatusCode);
        Client stored = Context.Clients.AsNoTracking().Single(client => client.Id == 1);
        Assert.AreEqual("Updated Client", stored.Name);
        Assert.AreEqual(TestData.CreatedAt, stored.CreatedAt);
        Assert.IsTrue(stored.UpdatedAt > TestData.UpdatedAt);
        Assert.IsTrue(stored.UpdatedAt < updated.UpdatedAt);
        Assert.IsFalse(Context.Clients.Any(client => client.Id == 9002));

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/clients/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/clients/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.IsFalse(Context.Clients.Any(client => client.Id == 1));
    }

    [TestMethod]
    public async Task DuplicateClientIdReturns409WithoutChangingStoredClientAsync()
    {
        await using WebApplication app = CreateApplication();
        Client duplicate = CreateClient(1);
        duplicate.Name = "Duplicate Client";

        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/clients",
            body: JsonSerializer.Serialize(duplicate), method: "POST")).Response.StatusCode);
        Assert.AreEqual(1, Context.Clients.Count());
        Assert.AreEqual("Test Client", Context.Clients.AsNoTracking().Single().Name);
    }

    [DataTestMethod]
    [DataRow("{")]
    [DataRow("[]")]
    [DataRow("null")]
    [DataRow("{}")]
    public async Task InvalidBodiesReturn400WithoutChangingStoredClientAsync(string body)
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/clients", body: body, method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/clients/{id}", "1", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(1, Context.Clients.Count());
        Assert.AreEqual("Test Client", Context.Clients.AsNoTracking().Single().Name);
    }

    [TestMethod]
    public async Task InvalidEmailAndNegativeCreationIdReturn400Async()
    {
        await using WebApplication app = CreateApplication();
        Client invalid = CreateClient();
        invalid.ContactEmail = "invalid-email";
        string body = JsonSerializer.Serialize(invalid);

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/clients", body: body, method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/clients/{id}", "1", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/clients",
            body: JsonSerializer.Serialize(CreateClient(-1)), method: "POST")).Response.StatusCode);
        Assert.AreEqual(1, Context.Clients.Count());
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<ClientAccess>()
        .AddTransient<ClientLogic>()
        .AddTransient<OrderDataAccess>()
        .AddTransient<OrderLogic>());

    private static Client CreateClient(int id = 0) => new()
    {
        Id = id,
        Name = "Test Client",
        Address = "Test Street 1",
        City = "Amersfoort",
        ZipCode = "3811AA",
        Province = "Utrecht",
        Country = "Netherlands",
        ContactName = "Test Contact",
        ContactPhone = "+31 600000000",
        ContactEmail = "test@example.com",
        CreatedAt = TestData.CreatedAt,
        UpdatedAt = TestData.UpdatedAt,
    };

    private static Order CreateOrder(int id, int clientId = 2, int shipToClientId = 2, int billToClientId = 2) => new()
    {
        Id = id,
        ClientId = clientId,
        ShipToClientId = shipToClientId,
        BillToClientId = billToClientId,
        Items = [new OrderItem { ItemId = 10, Amount = 5, UnitPrice = 2.50m }],
    };
}
