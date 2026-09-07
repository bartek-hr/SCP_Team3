using System.Text.Json;
using CargoHUB.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Framework;

[TestClass]
public sealed class ResponseTests
{
    [TestMethod]
    public void FactoryResponsesUseExpectedStatusCodes()
    {
        var responses = new (Response Response, int StatusCode)[]
        {
            (Response.Status(218), 218),
            (Response.NoContent(), StatusCodes.Status204NoContent),
            (Response.BadRequest(), StatusCodes.Status400BadRequest),
            (Response.Unauthorized(), StatusCodes.Status401Unauthorized),
            (Response.Forbidden(), StatusCodes.Status403Forbidden),
            (Response.NotFound(), StatusCodes.Status404NotFound),
            (Response.Conflict(), StatusCodes.Status409Conflict),
            (Response.Ok(), StatusCodes.Status200OK),
        };

        foreach (var (response, statusCode) in responses)
            Assert.AreEqual(statusCode, response.StatusCode);

        var payload = new Payload("box", 4);
        var typedResponses = new Response<Payload>[]
        {
            Response<Payload>.Ok(payload),
            Response<Payload>.Created(payload),
            Response<Payload>.Accepted(payload),
        };

        CollectionAssert.AreEqual(
            new[] { StatusCodes.Status200OK, StatusCodes.Status201Created, StatusCodes.Status202Accepted },
            typedResponses.Select(response => response.StatusCode).ToArray());
        Assert.AreSame(payload, typedResponses[0].Body);
        Assert.AreEqual(StatusCodes.Status404NotFound, Response<Payload>.NotFound().StatusCode);
        Assert.IsNull(Response<Payload>.NotFound().Body);
    }

    [TestMethod]
    public async Task ExecutesTypedResponsesWithStatusHeadersAndJsonBodyAsync()
    {
        var context = HttpContextTestHelpers.CreateContext();
        var response = Response.Ok(new Payload("box", 4))
            .Header("X-Trace", "trace-123")
            .CacheFor(TimeSpan.FromMinutes(5))
            .WithStatus(203);

        await response.ExecuteAsync(context);

        Assert.AreEqual(203, context.Response.StatusCode);
        Assert.AreEqual("trace-123", context.Response.Headers["X-Trace"].ToString());
        Assert.AreEqual("max-age=300", context.Response.Headers.CacheControl.ToString());

        using var document = JsonDocument.Parse(await HttpContextTestHelpers.ReadResponseBodyAsync(context));
        Assert.AreEqual("box", document.RootElement.GetProperty("name").GetString());
        Assert.AreEqual(4, document.RootElement.GetProperty("count").GetInt32());
    }

    [TestMethod]
    public async Task ExecutesEmptyResponsesWithoutWritingAResponseBodyAsync()
    {
        var context = HttpContextTestHelpers.CreateContext();
        var response = Response.NoContent().Header("X-Empty", "true");

        await response.ExecuteAsync(context);

        Assert.AreEqual(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.AreEqual("true", context.Response.Headers["X-Empty"].ToString());
        Assert.AreEqual(string.Empty, await HttpContextTestHelpers.ReadResponseBodyAsync(context));
    }

    private sealed record Payload(string Name, int Count);
}
