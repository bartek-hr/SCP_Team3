using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class ShipmentItemHandler(ShipmentItemLogic logic)
{
    [Get("/ShipmentItems/{id}")]
    [Describe("Geeft één record terug.", Tags = ["ShipmentItems"], OperationId = "getShipmentItem")]
    public Response<ShipmentItem> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<ShipmentItem>.BadRequest();
        }

        ShipmentItem? record = logic.GetById(id);
        if (record is null)
        {
            return Response<ShipmentItem>.NotFound();
        }

        return Response.Ok(record);
    }

    [Get("/ShipmentItems/{id}/items")]
    [Describe("Geeft de artikelen terug.", Tags = ["ShipmentItems"], OperationId = "GetShipmentItems")]
    public Response<IReadOnlyList<ShipmentItem>> GetItems(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<ShipmentItem>>.BadRequest();
        }

        return Response.Ok(logic.GetItems(id));
    }

    [Post("/ShipmentItems")]
    [Describe("Maakt een record aan.", Tags = ["ShipmentItems"], OperationId = "createShipmentItem", SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<ShipmentItem> request)
    {
        return WriteResponse.Save(() => logic.Add(request.Data), StatusCodes.Status201Created);
    }

    [Put("/ShipmentItems/{id}")]
    [Describe("Wijzigt een record.", Tags = ["ShipmentItems"], OperationId = "updateShipmentItem")]
    public Response Update(Request<ShipmentItem> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }
        if (logic.GetById(id) is null)
        {
            return Response.NotFound();
        }

        return WriteResponse.Save(() => logic.Update(id, request.Data), StatusCodes.Status200OK);
    }
    [Delete("/ShipmentItems/{id}")]
    [Describe("Verwijdert een record.", Tags = ["ShipmentItems"], OperationId = "deleteShipmentItem")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }
        if (logic.GetById(id) is null)
        {
            return Response.NotFound();
        }

        logic.Remove(id);
        return Response.Ok();
    }


}
