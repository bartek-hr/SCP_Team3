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
        Description = "The warehouse_id must refer to an existing warehouse.",
        Tags = ["Locations"],
        OperationId = "createLocation",
        SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<Location> request)
    {
        try
        {
            logic.Add(request.Data);
            return Response.Created();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
        catch (InvalidOperationException)
        {
            return Response.Conflict();
        }
    }

    [Put("/locations/{id}")]
    [Describe(
        "Updates a location.",
        Description = "The warehouse_id must refer to an existing warehouse.",
        Tags = ["Locations"],
        OperationId = "updateLocation")]
    public Response Update(Request<Location> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return logic.Update(id, request.Data) ? Response.Ok() : Response.NotFound();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Delete("/locations/{id}")]
    [Describe(
        "Deletes a location.",
        Description = "Returns 409 while transfers still use the location.",
        Tags = ["Locations"],
        OperationId = "deleteLocation")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return logic.Remove(id) ? Response.Ok() : Response.NotFound();
        }
        catch (InvalidOperationException)
        {
            return Response.Conflict();
        }
    }
}
