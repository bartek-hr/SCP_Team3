using CargoHUB.Framework;

namespace CargoHUB.Handlers;

public sealed class RouteDiscoveryTestHandler
{
    [Middleware("RouteBlock")]
    public Response Block()
    {
        CargoHUB.Tests.Framework.RouteInvocationLog.Add("block");
        return Response.Forbidden();
    }

    [Middleware("RouteBefore")]
    public Response? Before()
    {
        CargoHUB.Tests.Framework.RouteInvocationLog.Add("before");
        return null;
    }

    [Middleware("RouteAfter", Hook.After)]
    public Response? After()
    {
        CargoHUB.Tests.Framework.RouteInvocationLog.Add("after");
        return null;
    }

    [Middleware("RouteOverride", Hook.After)]
    public Response Override()
    {
        CargoHUB.Tests.Framework.RouteInvocationLog.Add("override-after");
        return Response.Conflict();
    }

    [Get("/discovered/{user}")]
    [Describe("Returns a greeting.")]
    public Response Hello(Request request) => Response.Ok($"Hello {request.Param("user")}!");

    [Get("/discovered/blocked")]
    [Describe("Returns a blocked response.")]
    [Use("RouteBlock")]
    public Response Blocked()
    {
        CargoHUB.Tests.Framework.RouteInvocationLog.Add("blocked-route");
        return Response.Ok();
    }

    [Get("/discovered/hooks")]
    [Describe("Executes route hooks.")]
    [Use("RouteBefore", "RouteAfter")]
    public Response Hooks()
    {
        CargoHUB.Tests.Framework.RouteInvocationLog.Add("route");
        return Response.Ok("completed");
    }

    [Get("/discovered/override")]
    [Describe("Executes an overriding route.")]
    [Use("RouteOverride")]
    public Response OverrideRoute()
    {
        CargoHUB.Tests.Framework.RouteInvocationLog.Add("override-route");
        return Response.Ok("route-result");
    }
}
