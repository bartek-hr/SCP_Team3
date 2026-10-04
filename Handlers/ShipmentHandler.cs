using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class ShipmentHandler(ShipmentLogic logic)
{
    [Get("/shipments")]
    [Describe("Geeft alle zendingen terug.", Tags = ["Shipments"], OperationId = "getShipments")]
    public Response<IReadOnlyList<Shipment>> GetAll() => Response.Ok(logic.GetAll());

    [Get("/shipments/{id}")]
    [Describe("Geeft één record terug.", Tags = ["Shipments"], OperationId = "getShipment")]
    public Response<Shipment> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<Shipment>.BadRequest();
        }

        Shipment? record = logic.GetById(id);
        if (record is null)
        {
            return Response<Shipment>.NotFound();
        }

        return Response.Ok(record);
    }

    [Get("/shipments/{id}/items")]
    [Describe("Geeft de artikelen terug.", Tags = ["Shipments"], OperationId = "getShipmentItems")]
    public Response<IReadOnlyList<ShipmentItem>> GetItems(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<ShipmentItem>>.BadRequest();
        }

        return Response.Ok(logic.GetItems(id));
    }

    [Post("/shipments")]
    [Describe("Maakt een record aan.", Tags = ["Shipments"], OperationId = "createShipment", SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<Shipment> request)
    {
        return WriteResponse.Save(() => logic.Add(request.Data), StatusCodes.Status201Created);
    }

    [Put("/shipments/{id}")]
    [Describe("Wijzigt een record.", Tags = ["Shipments"], OperationId = "updateShipment")]
    public Response Update(Request<Shipment> request)
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

    [Put("/shipments/{id}/items")]
    [Describe("Vervangt de artikelen en werkt de voorraad bij.", Tags = ["Shipments"], OperationId = "replaceShipmentItems")]
    public Response ReplaceItems(Request<List<ShipmentItem>> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }
        if (logic.GetById(id) is null)
        {
            return Response.NotFound();
        }

        return WriteResponse.Save(() => logic.ReplaceItems(id, request.Data), StatusCodes.Status200OK);
    }

    [Delete("/shipments/{id}")]
    [Describe("Verwijdert een record.", Tags = ["Shipments"], OperationId = "deleteShipment")]
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

    [Get("/shipments/{id}/orders")]
    [Describe("Geeft de gekoppelde order terug.", Tags = ["Shipments"], OperationId = "getShipmentOrders")]
    public Response<IReadOnlyList<int>> GetOrderIds(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<int>>.BadRequest();
        }

        return Response.Ok(logic.GetOrderIds(id));
    }

    [Put("/shipments/{id}/orders")]
    [Describe("Koppelt de eerste order uit de lijst; een lege lijst wist de koppeling.", Tags = ["Shipments"], OperationId = "replaceShipmentOrders")]
    public Response ReplaceOrders(Request<List<int>> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }
        if (logic.GetById(id) is null)
        {
            return Response.NotFound();
        }

        return WriteResponse.Save(() => logic.ReplaceOrders(id, request.Data), StatusCodes.Status200OK);
    }
}
