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
    [Describe(
        "Creates a warehouse.",
        Description = "All fields are required and contact_email must be a valid email address. Returns 409 when the id is already used.",
        Tags = ["Warehouses"],
        OperationId = "createWarehouse",
        SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<Warehouse> request)
    {
        return WriteResponse.Save(() => warehouseLogic.Add(request.Data), StatusCodes.Status201Created);
    }

    [Put("/warehouses/{id}")]
    [Describe(
        "Updates a warehouse.",
        Description = "Uses the same rules as creating a warehouse. An id in the body must match the URL.",
        Tags = ["Warehouses"],
        OperationId = "updateWarehouse")]
    public Response Update(Request<Warehouse> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        return WriteResponse.Save(() => warehouseLogic.Update(id, request.Data), StatusCodes.Status200OK);
    }

    [Delete("/warehouses/{id}")]
    [Describe(
        "Deletes a warehouse.",
        Description = "Returns 409 while the warehouse still has locations or is used by orders.",
        Tags = ["Warehouses"],
        OperationId = "deleteWarehouse")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        return WriteResponse.Save(() => warehouseLogic.Remove(id), StatusCodes.Status200OK);
    }
}
