using CargoHUB.Framework;

namespace CargoHUB.Handlers;

public class TestHandler
{

    [Middleware("Blocked")]
    public Response? Blocked() => Response.Forbidden();

    [Get("/test")]
    [Use("Blocked")]
    public Response Test() => Response.Ok();

    [Get("/hello/{user}")]
    public Response Test(Request request) => Response.Ok($"Hello {request.Param("user")}!");

}
