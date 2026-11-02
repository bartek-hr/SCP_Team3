using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CargoHUB.Access;
using CargoHUB.Datasource;
using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Framework;

[TestClass]
public sealed class ApiVersionJourneyTests : DatabaseTest
{
    [TestMethod]
    public async Task V1CrudAndContractRemainAvailableAlongsideAnIncompatibleV2Async()
    {
        await using var baseline = await CreateHostAsync(includeV2: false);
        using var baselineClient = baseline.GetTestClient();
        using var original = JsonDocument.Parse(await baselineClient.GetStringAsync("/openapi/v1.json"));

        await using var app = await CreateHostAsync(includeV2: true);
        using var client = app.GetTestClient();
        using var v1 = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        using var v2 = JsonDocument.Parse(await client.GetStringAsync("/openapi/v2.json"));
        Assert.IsTrue(original.RootElement.GetProperty("paths").TryGetProperty("/api/v1/clients/{id}", out _));
        foreach (var path in original.RootElement.GetProperty("paths").EnumerateObject())
            Assert.AreEqual(path.Value.GetRawText(), v1.RootElement.GetProperty("paths").GetProperty(path.Name).GetRawText(), path.Name);
        Assert.AreEqual(original.RootElement.TryGetProperty("components", out var originalSchemas),
            v1.RootElement.TryGetProperty("components", out var currentSchemas));
        if (originalSchemas.ValueKind != JsonValueKind.Undefined)
            Assert.AreEqual(originalSchemas.GetRawText(), currentSchemas.GetRawText());
        Assert.AreEqual("1.0", v1.RootElement.GetProperty("info").GetProperty("version").GetString());
        Assert.AreEqual("2.0", v2.RootElement.GetProperty("info").GetProperty("version").GetString());
        Assert.IsTrue(v1.RootElement.GetProperty("paths").EnumerateObject().All(path => path.Name.StartsWith("/api/v1/")));
        var v2Paths = v2.RootElement.GetProperty("paths").EnumerateObject().ToArray();
        Assert.AreEqual(2, v2Paths.Length);
        Assert.IsTrue(v2Paths.All(path => path.Name.StartsWith("/api/v2/")));
        Assert.AreEqual("getClients", v2Paths.Single(path => path.Name == "/api/v2/clients").Value.GetProperty("get").GetProperty("operationId").GetString());

        var body = new Client
        {
            Id = 82001, Name = "Existing integration", Address = "Street 1", City = "Utrecht",
            ZipCode = "1234AB", Province = "Utrecht", Country = "NL", ContactName = "Client",
            ContactPhone = "12345", ContactEmail = "client@example.com",
        };
        using var created = await client.PostAsJsonAsync("/api/v1/clients", body);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        using var fetched = JsonDocument.Parse(await client.GetStringAsync("/api/v1/clients/82001"));
        Assert.AreEqual("Existing integration", fetched.RootElement.GetProperty("name").GetString());
        Assert.IsTrue(fetched.RootElement.TryGetProperty("contact_email", out _));
        Assert.IsTrue((await client.GetFromJsonAsync<Client[]>("/api/v1/clients"))!.Any(value => value.Id == 82001));
        Assert.AreEqual("V2 envelope", (await client.GetFromJsonAsync<VersionEnvelope>("/api/v2/clients"))!.Message);
        body.Name = "Updated integration";
        using var updated = await client.PutAsJsonAsync("/api/v1/clients/82001", body);
        Assert.AreEqual(HttpStatusCode.OK, updated.StatusCode);
        Assert.AreEqual(body.Name, (await client.GetFromJsonAsync<Client>("/api/v1/clients/82001"))!.Name);
        using var malformed = await client.PostAsync("/api/v1/clients", new StringContent("{", System.Text.Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.BadRequest, malformed.StatusCode);
        using var deleted = await client.DeleteAsync("/api/v1/clients/82001");
        Assert.AreEqual(HttpStatusCode.OK, deleted.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/clients/82001")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/v2/warehouses")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/api/v3/clients")).StatusCode);
    }

    private async Task<WebApplication> CreateHostAsync(bool includeV2)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<CargoHubDbContext>(Context);
        builder.Services.AddTransient<ClientAccess>();
        builder.Services.AddTransient<ClientLogic>();
        builder.Services.AddVersionedOpenApi();
        var app = builder.Build();
        app.MapDiscoveredRoutes();
        if (includeV2)
            app.MapDiscoveredRoutes(typeof(CargoHUB.Handlers.V2.VersionJourneyHandler).Assembly);
        app.MapOpenApi();
        await app.StartAsync();
        return app;
    }
}

public sealed record VersionEnvelope(string Message);
