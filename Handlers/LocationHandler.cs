using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class LocationHandler(LocationLogic logic)
{
    [Get("/locations")]
    [Describe("Lists locations.", Tags = ["Locations"], OperationId = "getLocations")]
    public Response<IReadOnlyList<Location>> GetAll() => Response.Ok(logic.GetAll());

    [Get("/locations/{id}")]
    [Describe("Gets a location.", Tags = ["Locations"], OperationId = "getLocation")]
    public Response<Location> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<Location>.BadRequest();
        }

        Location? location = logic.GetById(id);
        return location is null ? Response<Location>.NotFound() : Response.Ok(location);
    }

    [Post("/locations")]
    [Describe(
        "Creates a location.",
        Description = "code and name are required and warehouse_id must refer to an existing warehouse. Returns 409 when the id is already used.",
        Tags = ["Locations"],
        OperationId = "createLocation",
        SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<Location> request)
    {
        return WriteResponse.Save(() => logic.Add(request.Data), StatusCodes.Status201Created);
    }

    [Put("/locations/{id}")]
    [Describe(
        "Updates a location.",
        Description = "Uses the same rules as creating a location. An id in the body must match the URL.",
        Tags = ["Locations"],
        OperationId = "updateLocation")]
    public Response Update(Request<Location> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        return WriteResponse.Save(() => logic.Update(id, request.Data), StatusCodes.Status200OK);
    }

    [Delete("/locations/{id}")]
    [Describe(
        "Deletes a location.",
        Description = "Returns 409 while transfers or inventories still use the location.",
        Tags = ["Locations"],
        OperationId = "deleteLocation")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        return WriteResponse.Save(() => logic.Remove(id), StatusCodes.Status200OK);
    }
}
