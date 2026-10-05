using System.Text.Json;
using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Handlers;

[TestClass]
public sealed class TransferHandlerTests : HandlerTest
{
    // Same shapes as the legacy POST /transfers and PUT /transfers/{id} request bodies.
    private const string LegacyPostBody = """
        {
          "id": 9001,
          "reference": "TRF-009001",
          "from_location_id": 1,
          "to_location_id": 2,
          "items": [
            { "item_id": 1, "amount": 10 },
            { "item_id": 2, "amount": 5 }
          ]
        }
        """;

    private const string LegacyPutBody = """
        {
          "id": 9001,
          "reference": "TRF-009001",
          "from_location_id": 1,
          "to_location_id": 3,
          "transfer_status": "Scheduled",
          "created_at": "2026-09-07T10:00:00Z",
          "items": [
            { "item_id": 1, "amount": 15 }
          ]
        }
        """;

    protected override void SeedDatabase()
    {
        Context.Warehouses.Add(TestData.CreateWarehouse(1));
        Context.Locations.AddRange(TestData.CreateLocation(1), TestData.CreateLocation(2), TestData.CreateLocation(3));

        Transfer processed = TestData.CreateTransfer(1, 1, 2, TestData.CreateItem(10, 5), TestData.CreateItem(20, 3));
        processed.TransferStatus = TransferStatus.Processed;
        Context.Transfers.Add(processed);
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task GetRoutesReturnTransfersWithTheLegacyItemShapeAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext list = await InvokeAsync(app, "/api/v1/transfers");
        Assert.AreEqual(200, list.Response.StatusCode);
        using JsonDocument listJson = await ReadJsonAsync(list);
        JsonElement transfer = listJson.RootElement[0];
        Assert.AreEqual("Processed", transfer.GetProperty("transfer_status").GetString());
        Assert.AreEqual(2, transfer.GetProperty("to_location_id").GetInt32());
        Assert.AreEqual(2, transfer.GetProperty("items").GetArrayLength());

        DefaultHttpContext single = await InvokeAsync(app, "/api/v1/transfers/{id}", "1");
        Assert.AreEqual(200, single.Response.StatusCode);
        using JsonDocument singleJson = await ReadJsonAsync(single);
        Assert.AreEqual("TRF-TEST", singleJson.RootElement.GetProperty("reference").GetString());

        DefaultHttpContext items = await InvokeAsync(app, "/api/v1/transfers/{id}/items", "1");
        Assert.AreEqual(200, items.Response.StatusCode);
        Assert.AreEqual(
            """[{"item_id":10,"amount":5},{"item_id":20,"amount":3}]""",
            await HttpContextTestHelpers.ReadResponseBodyAsync(items));

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/transfers/{id}", "abc")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/transfers/{id}", "999")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/transfers/{id}/items", "abc")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/transfers/{id}/items", "999")).Response.StatusCode);
    }

    [TestMethod]
    public async Task PostSchedulesTheTransferAndStoresItsItemsAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/transfers", body: LegacyPostBody, method: "POST")).Response.StatusCode);

        using JsonDocument json = await ReadJsonAsync(await InvokeAsync(app, "/api/v1/transfers/{id}", "9001"));
        Assert.AreEqual("Scheduled", json.RootElement.GetProperty("transfer_status").GetString());
        Assert.AreEqual(
            """[{"item_id":1,"amount":10},{"item_id":2,"amount":5}]""",
            json.RootElement.GetProperty("items").GetRawText());
        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/transfers", body: LegacyPostBody, method: "POST")).Response.StatusCode);
    }

    [TestMethod]
    public async Task PutReplacesOrKeepsItemsAndNeverChangesTheStatusAsync()
    {
        await using WebApplication app = CreateApplication();
        await InvokeAsync(app, "/api/v1/transfers", body: LegacyPostBody, method: "POST");

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/transfers/{id}", "9001", LegacyPutBody, "PUT")).Response.StatusCode);
        Assert.AreEqual(
            """[{"item_id":1,"amount":15}]""",
            await HttpContextTestHelpers.ReadResponseBodyAsync(await InvokeAsync(app, "/api/v1/transfers/{id}/items", "9001")));

        string withoutItems = """
            { "reference": "TRF-009001-B", "from_location_id": 2, "to_location_id": 3, "transfer_status": "Processed" }
            """;
        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/transfers/{id}", "9001", withoutItems, "PUT")).Response.StatusCode);

        using JsonDocument json = await ReadJsonAsync(await InvokeAsync(app, "/api/v1/transfers/{id}", "9001"));
        Assert.AreEqual("TRF-009001-B", json.RootElement.GetProperty("reference").GetString());
        Assert.AreEqual("Scheduled", json.RootElement.GetProperty("transfer_status").GetString());
        Assert.AreEqual(1, json.RootElement.GetProperty("items").GetArrayLength());

        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/transfers/{id}", "999", LegacyPutBody, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/transfers/{id}", "abc", LegacyPutBody, "PUT")).Response.StatusCode);
    }

    [TestMethod]
    public async Task CommitAnswers501UntilInventoriesExistAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(501, (await InvokeAsync(app, "/api/v1/transfers/{id}/commit", "1", method: "PUT")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/transfers/{id}/commit", "999", method: "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/transfers/{id}/commit", "abc", method: "PUT")).Response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteRemovesTheTransferAndItsItemsAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/transfers/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(0, Context.Transfers.Count());
        Assert.AreEqual(0, Context.TransferItems.Count());
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/transfers/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/transfers/{id}", "abc", method: "DELETE")).Response.StatusCode);
    }

    [TestMethod]
    public async Task InvalidTransfersReturn400Async()
    {
        await using WebApplication app = CreateApplication();
        string[] invalidBodies =
        [
            """{ "reference": "TRF-X", "from_location_id": 1, "to_location_id": 1 }""",
            """{ "reference": "TRF-X", "from_location_id": 1, "to_location_id": 999 }""",
            """{ "reference": "", "from_location_id": 1, "to_location_id": 2 }""",
            """{ "reference": "TRF-X", "from_location_id": 1, "to_location_id": 2, "items": [{ "item_id": 1, "amount": 0 }] }""",
            """{ "reference": "TRF-X", "from_location_id": 1, "to_location_id": 2, "transfer_status": "Pending" }""",
            "{",
        ];

        foreach (string body in invalidBodies)
        {
            Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/transfers", body: body, method: "POST")).Response.StatusCode, body);
        }

        Assert.AreEqual(1, Context.Transfers.Count());
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<TransferAccess>()
        .AddTransient<LocationAccess>()
        .AddTransient<TransferLogic>());
}
