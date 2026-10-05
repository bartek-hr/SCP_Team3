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
public sealed class SupplierHandlerTests : HandlerTest
{
    protected override void SeedDatabase()
    {
        Context.Suppliers.Add(new Supplier
        {
            Id = 1,
            Code = "SUP-0001",
            Name = "Test Supplier",
            Address = "Street 1",
            City = "Amersfoort",
            ZipCode = "3811AA",
            Province = "Utrecht",
            Country = "Netherlands",
            ContactName = "Test Contact",
            PhoneNumber = "123456789",
            Reference = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        Context.SaveChanges();
    }

    [TestMethod]
    public async Task GetRoutesReturnJsonAndExpectedErrorsAsync()
    {
        await using WebApplication app = CreateApplication();

        DefaultHttpContext list = await InvokeAsync(app, "/api/v1/suppliers");
        Assert.AreEqual(200, list.Response.StatusCode);
        using JsonDocument listJson = JsonDocument.Parse(await HttpContextTestHelpers.ReadResponseBodyAsync(list));
        Assert.AreEqual("SUP-0001", listJson.RootElement[0].GetProperty("code").GetString());
        Assert.AreEqual("123456789", listJson.RootElement[0].GetProperty("phone_number").GetString());

        DefaultHttpContext single = await InvokeAsync(app, "/api/v1/suppliers/{id}", "1");
        Assert.AreEqual(200, single.Response.StatusCode);
        using JsonDocument singleJson = JsonDocument.Parse(await HttpContextTestHelpers.ReadResponseBodyAsync(single));
        Assert.AreEqual("Test Supplier", singleJson.RootElement.GetProperty("name").GetString());

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "abc")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "999")).Response.StatusCode);
    }

    [TestMethod]
    public async Task ItemsRouteReturns501OnlyForExistingSupplierAsync()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(501, (await InvokeAsync(app, "/api/v1/suppliers/{id}/items", "1")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/suppliers/{id}/items", "999")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}/items", "abc")).Response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUpdateDeleteAndMissingRecordsUseExpectedStatusesAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(new Supplier
        {
            Code = "SUP-0002",
            Name = "New Supplier",
            Address = "Street 2",
            City = "Amsterdam",
            ZipCode = "1000AA",
            Province = "Noord-Holland",
            Country = "Netherlands",
            ContactName = "New Contact",
            PhoneNumber = "987654321",
            Reference = "New"
        });

        Assert.AreEqual(201, (await InvokeAsync(app, "/api/v1/suppliers", body: body, method: "POST")).Response.StatusCode);
        Supplier created = Context.Suppliers.Single(supplier => supplier.Code == "SUP-0002");
        Assert.IsTrue(created.CreatedAt > DateTime.UnixEpoch);

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "1", body, "PUT")).Response.StatusCode);
        Assert.AreEqual("New Supplier", Context.Suppliers.Find(1)?.Name);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "999", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "abc", body, "PUT")).Response.StatusCode);

        Assert.AreEqual(200, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(404, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "1", method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "abc", method: "DELETE")).Response.StatusCode);
    }

    [TestMethod]
    public async Task InvalidRequestBodiesReturn400Async()
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers", body: "{", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers", body: "[]", method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "1", "{", "PUT")).Response.StatusCode);
    }

    [DataTestMethod]
    [DataRow("abc")]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("2147483648")]
    public async Task InvalidIdsReturn400WithoutChangingStoredRecordsAsync(string id)
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(CreateDuplicateSupplier());

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", id)).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}/items", id)).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", id, body, "PUT")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", id, method: "DELETE")).Response.StatusCode);
        Assert.AreEqual(1, Context.Suppliers.Count());
    }

    [TestMethod]
    public async Task DuplicateIdReturns409WithoutChangingStoredRecordAsync()
    {
        await using WebApplication app = CreateApplication();
        string body = JsonSerializer.Serialize(CreateDuplicateSupplier());

        Assert.AreEqual(409, (await InvokeAsync(app, "/api/v1/suppliers", body: body, method: "POST")).Response.StatusCode);
        Assert.AreEqual(1, Context.Suppliers.Count());
        Assert.AreEqual("Test Supplier", Context.Suppliers.AsNoTracking().Single().Name);
    }

    [DataTestMethod]
    [DataRow("null")]
    [DataRow("{}")]
    public async Task InvalidDataReturns400WithoutChangingStoredRecordAsync(string body)
    {
        await using WebApplication app = CreateApplication();

        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers", body: body, method: "POST")).Response.StatusCode);
        Assert.AreEqual(400, (await InvokeAsync(app, "/api/v1/suppliers/{id}", "1", body, "PUT")).Response.StatusCode);
        Assert.AreEqual(1, Context.Suppliers.Count());
        Assert.AreEqual("Test Supplier", Context.Suppliers.AsNoTracking().Single().Name);
    }

    private WebApplication CreateApplication() => CreateApplication(services => services
        .AddTransient<SupplierAccess>()
        .AddTransient<SupplierLogic>());

    private static Supplier CreateDuplicateSupplier() => new()
    {
        Id = 1,
        Code = "SUP-DUPLICATE",
        Name = "Duplicate Supplier",
        Address = "Street 2",
        City = "Amsterdam",
        ZipCode = "1000AA",
        Province = "Noord-Holland",
        Country = "Netherlands",
        ContactName = "New Contact",
        PhoneNumber = "987654321",
        Reference = "New"
    };
}
