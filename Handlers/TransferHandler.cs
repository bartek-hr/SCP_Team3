using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class TransferHandler(TransferLogic logic)
{
    [Get("/transfers")]
    [Describe("Lists transfers with their items.", Tags = ["Transfers"], OperationId = "getTransfers")]
    public Response<IReadOnlyList<Transfer>> GetAll() => Response.Ok(logic.GetAll());

    [Get("/transfers/{id}")]
    [Describe("Gets a transfer with its items.", Tags = ["Transfers"], OperationId = "getTransfer")]
    public Response<Transfer> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<Transfer>.BadRequest();
        }

        Transfer? transfer = logic.GetById(id);
        return transfer is null ? Response<Transfer>.NotFound() : Response.Ok(transfer);
    }

    [Get("/transfers/{id}/items")]
    [Describe("Lists the items in a transfer.", Tags = ["Transfers"], OperationId = "getTransferItems")]
    public Response<IReadOnlyList<TransferItem>> GetItems(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<TransferItem>>.BadRequest();
        }

        IReadOnlyList<TransferItem>? items = logic.GetItems(id);
        return items is null ? Response<IReadOnlyList<TransferItem>>.NotFound() : Response.Ok(items);
    }

    [Post("/transfers")]
    [Describe(
        "Creates a transfer.",
        Description = "A new transfer always starts with transfer_status Scheduled.",
        Tags = ["Transfers"],
        OperationId = "createTransfer",
        SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<Transfer> request)
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

    [Put("/transfers/{id}")]
    [Describe(
        "Updates a transfer.",
        Description = "Omit items to keep the stored items. The transfer_status is kept; it only changes by committing.",
        Tags = ["Transfers"],
        OperationId = "updateTransfer")]
    public Response Update(Request<Transfer> request)
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

    [Put("/transfers/{id}/commit")]
    [Describe(
        "Commits a transfer.",
        Description = "Moves the stock to the destination location. Returns 501 until inventories are available in this API.",
        Tags = ["Transfers"],
        OperationId = "commitTransfer")]
    public Response Commit(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        return logic.GetById(id) is null ? Response.NotFound() : Response.NotImplemented();
    }

    [Delete("/transfers/{id}")]
    [Describe("Deletes a transfer and its items.", Tags = ["Transfers"], OperationId = "deleteTransfer")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        return logic.Remove(id) ? Response.Ok() : Response.NotFound();
    }
}
