using System.Text.Json;
using CargoHUB.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Handlers;

[TestClass]
public sealed class ModelHandlerOpenApiTests
{
    [DataTestMethod]
    [DataRow("/api/v1/suppliers/{id}", "Supplier", "phone_number", "reference")]
    [DataRow("/api/v1/item_types/{id}", "ItemType", "name", "description")]
    public async Task GetByIdDocumentsTheModelResponseSchemaAsync(
        string route, string modelName, string firstField, string secondField)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        // A nonliteral name keeps XML-comment interceptors out of this test project.
        string documentName = "v1";
        builder.Services.AddOpenApi(documentName);
        await using WebApplication app = builder.Build();
        app.MapDiscoveredRoutes();
        app.MapOpenApi();
        await app.StartAsync();
        using HttpClient client = new() { BaseAddress = new Uri(app.Urls.Single()) };
        using JsonDocument document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        JsonElement responses = document.RootElement.GetProperty("paths")
            .GetProperty(route).GetProperty("get").GetProperty("responses");
        Assert.IsTrue(responses.GetProperty("200").TryGetProperty("content", out JsonElement content),
            $"GET {route} must document its JSON response body.");
        JsonElement schema = content.GetProperty("application/json").GetProperty("schema");
        if (schema.TryGetProperty("$ref", out JsonElement reference))
        {
            Assert.AreEqual($"#/components/schemas/{modelName}", reference.GetString());
            schema = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(modelName);
        }

        AssertSchemaType(schema, "object");
        JsonElement properties = schema.GetProperty("properties");
        AssertSchemaType(properties.GetProperty("id"), "integer");
        AssertSchemaType(properties.GetProperty(firstField), "string");
        AssertSchemaType(properties.GetProperty(secondField), "string");
        Assert.AreEqual("date-time", properties.GetProperty("created_at").GetProperty("format").GetString());
        Assert.AreEqual("date-time", properties.GetProperty("updated_at").GetProperty("format").GetString());
        Assert.IsFalse(responses.GetProperty("404").TryGetProperty("content", out _));
    }

    private static void AssertSchemaType(JsonElement schema, string expectedType)
    {
        JsonElement type = schema.GetProperty("type");
        if (type.ValueKind == JsonValueKind.Array)
        {
            CollectionAssert.Contains(type.EnumerateArray().Select(value => value.GetString()).ToArray(), expectedType);
        }
        else
        {
            Assert.AreEqual(expectedType, type.GetString());
        }
    }
}
