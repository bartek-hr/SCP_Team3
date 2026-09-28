using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class WarehouseHandler(WarehouseLogic warehouseLogic, LocationLogic locationLogic)
{
    [Get("/warehouses")]
    [Describe("Lists warehouses.", Tags = ["Warehouses"], OperationId = "getWarehouses")]
    public Response<IReadOnlyList<Warehouse>> GetAll() => Response.Ok(warehouseLogic.GetAll());

    [Get("/warehouses/{id}")]
    [Describe("Gets a warehouse.", Tags = ["Warehouses"], OperationId = "getWarehouse")]
    public Response<Warehouse> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<Warehouse>.BadRequest();
        }

        Warehouse? warehouse = warehouseLogic.GetById(id);
        return warehouse is null ? Response<Warehouse>.NotFound() : Response.Ok(warehouse);
    }

    [Get("/warehouses/{id}/locations")]
    [Describe("Lists the locations in a warehouse.", Tags = ["Warehouses"], OperationId = "getWarehouseLocations")]
    public Response<IReadOnlyList<Location>> GetLocations(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<Location>>.BadRequest();
        }

        if (warehouseLogic.GetById(id) is null)
        {
            return Response<IReadOnlyList<Location>>.NotFound();
        }

        return Response.Ok(locationLogic.GetByWarehouseId(id));
    }

    [Post("/warehouses")]
    [Describe("Creates a warehouse.", Tags = ["Warehouses"], OperationId = "createWarehouse", SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<Warehouse> request)
    {
        try
        {
            warehouseLogic.Add(request.Data);
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

    [Put("/warehouses/{id}")]
    [Describe("Updates a warehouse.", Tags = ["Warehouses"], OperationId = "updateWarehouse")]
    public Response Update(Request<Warehouse> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return warehouseLogic.Update(id, request.Data) ? Response.Ok() : Response.NotFound();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Delete("/warehouses/{id}")]
    [Describe(
        "Deletes a warehouse.",
        Description = "Returns 409 while the warehouse still has locations.",
        Tags = ["Warehouses"],
        OperationId = "deleteWarehouse")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return warehouseLogic.Remove(id) ? Response.Ok() : Response.NotFound();
        }
        catch (InvalidOperationException)
        {
            return Response.Conflict();
        }
    }
}
