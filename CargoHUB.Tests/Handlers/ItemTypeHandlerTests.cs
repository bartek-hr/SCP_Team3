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
public sealed class ItemTypeHandlerTests : HandlerTest
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
    public async Task ItemsRouteReturnsEmptyArrayAndExpectedErrorsAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext response = await InvokeAsync(app, "/api/v1/item_types/{id}/items", "1");
        Assert.AreEqual(200, response.Response.StatusCode);
        Assert.AreEqual("[]", await HttpContextTestHelpers.ReadResponseBodyAsync(response));
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_types/{id}/items", "999")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}/items", "abc")).Response.StatusCode);
    }

    [TestMethod]
    public async Task ItemsRouteReturnsOnlyMatchingItemIdsInOrderAcrossSuppliersAsync()
    {
        Context.ItemLines.Add(new ItemLine { Id = 1, Name = "Test Line" });
        Context.ItemGroups.Add(new ItemGroup { Id = 1, Name = "Test Group" });
        Context.Items.AddRange(
            CreateItem(3, itemTypeId: 1, supplierId: 1),
            CreateItem(2, itemTypeId: 2),
            CreateItem(1, itemTypeId: 1, supplierId: 2),
            CreateItem(4, itemTypeId: 999));
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        await using WebApplication app = CreateApplication();

        DefaultHttpContext response = await InvokeAsync(app, "/api/v1/item_types/{id}/items", "1");

        Assert.AreEqual(200, response.Response.StatusCode);
        using JsonDocument json = await ReadJsonAsync(response);
        CollectionAssert.AreEqual(new[] { 1, 3 }, json.RootElement.EnumerateArray()
            .Select(itemId => itemId.GetInt32()).ToArray());
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_types/{id}/items", "999")).Response.StatusCode);
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

    [DataTestMethod]
    [DataRow("abc")]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("2147483648")]
    public async Task InvalidIdsReturn400WithoutChangingStoredRecordsAsync(string id)
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(CreateItemType(1));

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}", id)).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}/items", id)).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}", id, body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}", id, method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(1, Context.ItemTypes.Count());
    }

    [TestMethod]
    public async Task DuplicateIdReturns409WithoutChangingStoredRecordAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(CreateItemType(1));

        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/item_types", body: body, method: "POST")).Response.StatusCode);
        Assert.AreEqual(1, Context.ItemTypes.Count());
        Assert.AreEqual("Single", Context.ItemTypes.AsNoTracking().Single().Name);
    }

    [DataTestMethod]
    [DataRow("null")]
    [DataRow("{}")]
    public async Task InvalidDataReturns400WithoutChangingStoredRecordAsync(string body)
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types", body: body, method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_types/{id}", "1", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(1, Context.ItemTypes.Count());
        Assert.AreEqual("Single", Context.ItemTypes.AsNoTracking().Single().Name);
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<ItemTypeAccess>()
        .AddTransient<ItemTypeLogic>()
        .AddTransient<ItemAccess>()
        .AddTransient<ItemLineAccess>()
        .AddTransient<ItemGroupAccess>()
        .AddTransient<ItemLogic>());

    private static Item CreateItem(int id, int itemTypeId, int supplierId = 1) => new()
    {
        Id = id,
        Code = $"ITEM-{id}",
        Description = $"Test Item {id}",
        ItemLineId = 1,
        ItemGroupId = 1,
        ItemTypeId = itemTypeId,
        SupplierId = supplierId,
    };

    private static ItemType CreateItemType(int id = 0) => new()
    {
        Id = id,
        Name = "Single",
        Description = "Packaging tier: Single",
    };
}
