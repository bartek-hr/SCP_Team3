using System.Security.Claims;
using System.Text;
using CargoHUB.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Framework;

[TestClass]
public sealed class RequestTests
{
    [TestMethod]
    public void ExposesHttpContextDataAndParsesRouteQueryAndHeaderValues()
    {
        var context = HttpContextTestHelpers.CreateContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/orders/42";
        context.Request.RouteValues["id"] = "42";
        context.Request.QueryString = new QueryString("?page=3");
        context.Request.Headers["X-Trace"] = "trace-123";
        context.RequestAborted = new CancellationToken(canceled: false);
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "Ada")],
            authenticationType: "test"));

        var request = new Request(context);

        Assert.AreSame(context, request.Context);
        Assert.AreSame(context.Request, request.Raw);
        Assert.AreEqual(HttpMethods.Post, request.Method);
        Assert.AreEqual(new PathString("/orders/42"), request.Path);
        Assert.AreSame(context.Request.RouteValues, request.Params);
        Assert.AreSame(context.Request.Headers, request.Headers);
        Assert.AreSame(context.Request.Query, request.QueryCollection);
        Assert.AreSame(context.User, request.User);
        Assert.AreSame(context.RequestServices, request.Services);
        Assert.AreEqual(context.RequestAborted, request.Aborted);

        Assert.AreEqual("42", request.Param("id"));
        Assert.AreEqual(42, request.Param<int>("id"));
        Assert.IsTrue(request.TryParam<int>("id", out var id));
        Assert.AreEqual(42, id);

        Assert.AreEqual("3", request.Query("page"));
        Assert.AreEqual(3, request.Query<int>("page"));
        Assert.AreEqual("trace-123", request.Header("X-Trace"));
    }

    [TestMethod]
    public void ReturnsFallbacksForMissingOrInvalidOptionalValues()
    {
        var context = HttpContextTestHelpers.CreateContext();
        context.Request.RouteValues["invalid"] = "not-an-integer";
        var request = new Request(context);

        Assert.IsNull(request.Query("missing"));
        Assert.AreEqual(9, request.Query<int>("missing", 9));
        Assert.AreEqual(0, request.Query<int>("missing"));
        Assert.IsNull(request.Header("missing"));

        Assert.IsFalse(request.TryParam<int>("missing", out _));
        Assert.IsFalse(request.TryParam<int>("invalid", out _));
    }

    [TestMethod]
    public void ThrowsAnInformativeExceptionForMissingRequiredRouteParameters()
    {
        var context = HttpContextTestHelpers.CreateContext();
        context.Request.Path = "/orders";
        var request = new Request(context);

        var exception = Assert.ThrowsException<KeyNotFoundException>(() => request.Param("id"));

        StringAssert.Contains(exception.Message, "Route parameter 'id' is not present");
        StringAssert.Contains(exception.Message, "/orders");
    }

    [TestMethod]
    public async Task ReadsJsonBodiesAndRejectsMissingRequiredBodiesAsync()
    {
        var context = HttpContextTestHelpers.CreateContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"name\":\"box\",\"quantity\":4}"));
        var request = new Request(context);

        var body = await request.BodyAsync<BodyDto>();

        Assert.IsNotNull(body);
        Assert.AreEqual("box", body.Name);
        Assert.AreEqual(4, body.Quantity);

        var emptyContext = HttpContextTestHelpers.CreateContext();
        emptyContext.Request.ContentType = "application/json";
        emptyContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("null"));

        var exception = await Assert.ThrowsExceptionAsync<BadHttpRequestException>(
            async () => await new Request(emptyContext).RequiredBodyAsync<BodyDto>());

        StringAssert.Contains(exception.Message, "A JSON body of type BodyDto is required");
    }

    private sealed record BodyDto(string Name, int Quantity);
}
