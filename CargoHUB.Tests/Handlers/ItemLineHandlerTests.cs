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
public sealed class ItemLineHandlerTests : HandlerTest
{
    // Same shape as the legacy POST /item_lines request body.
    private const string LegacyPostBody = """
        {
          "id": 9001,
          "name": "Test Line",
          "description": "Created by a test"
        }
        """;

    protected override void SeedDatabase()
    {
        Context.ItemLines.AddRange(TestData.CreateItemLine(1), TestData.CreateItemLine(2, "Empty Line"));
        Context.ItemGroups.Add(TestData.CreateItemGroup(1));
        Context.Items.AddRange(
            TestData.CreateCatalogItem(1, itemLineId: 1, itemGroupId: 1),
            TestData.CreateCatalogItem(2, itemLineId: 1, itemGroupId: 1, code: "TST-ITEM-2"));
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task GetRoutesReturnJsonAndExpectedErrorsAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext list = await InvokeAsync(app, "/api/v1/item_lines");
        Assert.AreEqual(200, list.Response.StatusCode);
        using JsonDocument listJson = await ReadJsonAsync(list);
        Assert.AreEqual(2, listJson.RootElement.GetArrayLength());
        Assert.AreEqual("Tech Gadgets", listJson.RootElement[0].GetProperty("name").GetString());

        DefaultHttpContext single = await InvokeAsync(app, "/api/v1/item_lines/{id}", "2");
        Assert.AreEqual(200, single.Response.StatusCode);
        using JsonDocument singleJson = await ReadJsonAsync(single);
        Assert.AreEqual("Empty Line", singleJson.RootElement.GetProperty("name").GetString());
        Assert.IsTrue(singleJson.RootElement.TryGetProperty("created_at", out _));

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "abc")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "0")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "999")).Response.StatusCode);
    }

    [TestMethod]
    public async Task ItemsRouteListsTheItemIdsOfAnItemLineAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext items = await InvokeAsync(app, "/api/v1/item_lines/{id}/items", "1");
        Assert.AreEqual(200, items.Response.StatusCode);
        using JsonDocument json = await ReadJsonAsync(items);
        CollectionAssert.AreEqual(new[] { 1, 2 }, json.RootElement.EnumerateArray().Select(id => id.GetInt32()).ToArray());

        DefaultHttpContext empty = await InvokeAsync(app, "/api/v1/item_lines/{id}/items", "2");
        Assert.AreEqual(200, empty.Response.StatusCode);
        Assert.AreEqual("[]", await HttpContextTestHelpers.ReadResponseBodyAsync(empty));

        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_lines/{id}/items", "999")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_lines/{id}/items", "abc")).Response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUpdateAndDeleteUseExpectedStatusesAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/item_lines", body: LegacyPostBody, method: "POST")).Response.StatusCode);
        ItemLine created = Context.ItemLines.AsNoTracking().Single(itemLine => itemLine.Id == 9001);
        Assert.AreEqual("Test Line", created.Name);
        Assert.IsTrue(created.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/item_lines", body: LegacyPostBody, method: "POST")).Response.StatusCode);

        string body = JsonSerializer.Serialize(TestData.CreateItemLine(name: "Renamed Line"));
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "1", body, "PUT")).Response.StatusCode);
        Assert.AreEqual("Renamed Line", Context.ItemLines.AsNoTracking().Single(itemLine => itemLine.Id == 1).Name);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "999", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "abc", body, "PUT")).Response.StatusCode);

        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "2", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "2", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "abc", method: "DELETE")).Response.StatusCode);
    }

    [TestMethod]
    public async Task InvalidRequestBodiesReturn400Async()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_lines", body: "{", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_lines", body: "[]", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_lines", body: "{\"description\":\"No name\"}", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/item_lines/{id}", "1", "{", "PUT")).Response.StatusCode);
        Assert.AreEqual(2, Context.ItemLines.Count());
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<ItemAccess>()
        .AddTransient<ItemLineAccess>()
        .AddTransient<ItemGroupAccess>()
        .AddTransient<ItemLogic>()
        .AddTransient<ItemLineLogic>());
}
