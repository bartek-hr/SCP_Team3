using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class TransferItemHandler(TransferItemLogic logic)
{
    [Get("/TransferItems/{id}")]
    [Describe("Geeft één record terug.", Tags = ["TransferItems"], OperationId = "getTransferItem")]
    public Response<TransferItem> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<TransferItem>.BadRequest();
        }

        TransferItem? record = logic.GetById(id);
        if (record is null)
        {
            return Response<TransferItem>.NotFound();
        }

        return Response.Ok(record);
    }

    [Get("/TransferItems/{id}/items")]
    [Describe("Geeft de artikelen terug.", Tags = ["TransferItems"], OperationId = "GetTransferItems")]
    public Response<IReadOnlyList<TransferItem>> GetItems(Request request)
    {
        if (!request.TryParam<int>("shipment_id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<TransferItem>>.BadRequest();
        }

        return Response.Ok(logic.GetItems(id));
    }

    [Post("/TransferItems")]
    [Describe("Maakt een record aan.", Tags = ["TransferItems"], OperationId = "createTransferItem", SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<TransferItem> request)
    {
        return WriteResponse.Save(() => logic.Add(request.Data), StatusCodes.Status201Created);
    }

    [Put("/TransferItems/{id}")]
    [Describe("Wijzigt een record.", Tags = ["TransferItems"], OperationId = "updateTransferItem")]
    public Response Update(Request<TransferItem> request)
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
    [Delete("/TransferItems/{id}")]
    [Describe("Verwijdert een record.", Tags = ["TransferItems"], OperationId = "deleteTransferItem")]
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
