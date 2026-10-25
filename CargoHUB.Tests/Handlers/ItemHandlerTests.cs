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
public sealed class ItemHandlerTests : HandlerTest
{
    // Same shape as the legacy POST /items request body.
    private const string LegacyPostBody = """
        {
          "id": 9001,
          "code": "TST-ITEM-9001",
          "description": "Created by a test",
          "barcode": "0000000000017",
          "model_number": "TM-9001",
          "commodity_code": 4321,
          "unit_weight": 2.75,
          "item_line_id": 1,
          "item_group_id": 1,
          "item_type_id": 1,
          "min_purchase_qty": 5,
          "case_size": 20,
          "packaging_type": "Pallet",
          "order_multiple": 5,
          "supplier_id": 1,
          "supplier_sku": "SUP-SKU-9001"
        }
        """;

    protected override void SeedDatabase()
    {
        Context.ItemLines.Add(TestData.CreateItemLine(1));
        Context.ItemGroups.Add(TestData.CreateItemGroup(1));
        Context.Items.AddRange(
            TestData.CreateCatalogItem(1),
            TestData.CreateCatalogItem(2, code: "TST-ITEM-2"));
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task GetRoutesReturnJsonAndExpectedErrorsAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext list = await InvokeAsync(app, "/api/v1/items");
        Assert.AreEqual(200, list.Response.StatusCode);
        using JsonDocument listJson = await ReadJsonAsync(list);
        Assert.AreEqual(2, listJson.RootElement.GetArrayLength());
        Assert.AreEqual("TST-ITEM", listJson.RootElement[0].GetProperty("code").GetString());
        Assert.AreEqual("0012345678905", listJson.RootElement[0].GetProperty("barcode").GetString());
        Assert.AreEqual(1, listJson.RootElement[0].GetProperty("item_line_id").GetInt32());

        DefaultHttpContext single = await InvokeAsync(app, "/api/v1/items/{id}", "2");
        Assert.AreEqual(200, single.Response.StatusCode);
        using JsonDocument singleJson = await ReadJsonAsync(single);
        Assert.AreEqual("TST-ITEM-2", singleJson.RootElement.GetProperty("code").GetString());
        Assert.AreEqual(1.5m, singleJson.RootElement.GetProperty("unit_weight").GetDecimal());

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items/{id}", "abc")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items/{id}", "0")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/items/{id}", "999")).Response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUpdateAndDeleteUseExpectedStatusesAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/items", body: LegacyPostBody, method: "POST")).Response.StatusCode);
        Item created = Context.Items.AsNoTracking().Single(item => item.Id == 9001);
        Assert.AreEqual("TST-ITEM-9001", created.Code);
        Assert.AreEqual("0000000000017", created.Barcode);
        Assert.AreEqual(2.75m, created.UnitWeight);
        Assert.IsTrue(created.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/items", body: LegacyPostBody, method: "POST")).Response.StatusCode);

        string body = JsonSerializer.Serialize(TestData.CreateCatalogItem(code: "RENAMED"));
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/items/{id}", "1", body, "PUT")).Response.StatusCode);
        Assert.AreEqual("RENAMED", Context.Items.AsNoTracking().Single(item => item.Id == 1).Code);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/items/{id}", "999", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items/{id}", "abc", body, "PUT")).Response.StatusCode);

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/items/{id}", "2", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/items/{id}", "2", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items/{id}", "abc", method: "DELETE")).Response.StatusCode);
    }

    [TestMethod]
    public async Task InvalidRequestBodiesReturn400Async()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items", body: "{", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items", body: "[]", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items", body: "{\"code\":\"No description\"}", method: "POST")).Response.StatusCode);
        string unknownLineBody = JsonSerializer.Serialize(TestData.CreateCatalogItem(itemLineId: 999));
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items", body: unknownLineBody, method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items/{id}", "1", unknownLineBody, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/items/{id}", "1", "{", "PUT")).Response.StatusCode);
        Assert.AreEqual(2, Context.Items.Count());
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<ItemAccess>()
        .AddTransient<ItemLineAccess>()
        .AddTransient<ItemGroupAccess>()
        .AddTransient<ItemLogic>());
}
