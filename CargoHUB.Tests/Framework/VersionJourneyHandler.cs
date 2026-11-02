using CargoHUB.Framework;
using CargoHUB.Tests.Framework;

namespace CargoHUB.Handlers.V2;

// Test-only future contract: same resource and operation ID, incompatible body.
public sealed class VersionJourneyHandler
{
    [Get("/clients", version: 2)]
    [Describe("V2 clients fixture.", OperationId = "getClients")]
    public Response<VersionEnvelope> Get() => Response.Ok(new VersionEnvelope("V2 envelope"));
    [Get("/version-probe")]
    [Get("/version-probe", version: 2)]
    [Describe("A contract shared explicitly across versions.", OperationId = "versionProbe")]
    public Response Probe() => Response.Ok();
}
