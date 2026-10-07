using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class ItemLineHandler(ItemLineLogic itemLineLogic, ItemLogic itemLogic)
{
    [Get("/item_lines")]
    [Describe("Lists item lines.", Tags = ["Item Lines"], OperationId = "getItemLines")]
    public Response<IReadOnlyList<ItemLine>> GetAll() => Response.Ok(itemLineLogic.GetAll());

    [Get("/item_lines/{id}")]
    [Describe("Gets an item line.", Tags = ["Item Lines"], OperationId = "getItemLine")]
    public Response<ItemLine> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<ItemLine>.BadRequest();
        }

        ItemLine? itemLine = itemLineLogic.GetById(id);
        return itemLine is null ? Response<ItemLine>.NotFound() : Response.Ok(itemLine);
    }

    [Get("/item_lines/{id}/items")]
    [Describe("Lists the IDs of the items in an item line.", Tags = ["Item Lines"], OperationId = "getItemLineItems")]
    public Response<IReadOnlyList<int>> GetItems(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<int>>.BadRequest();
        }

        if (itemLineLogic.GetById(id) is null)
        {
            return Response<IReadOnlyList<int>>.NotFound();
        }

        return Response.Ok(itemLogic.GetIdsForItemLine(id));
    }

    [Post("/item_lines")]
    [Describe("Creates an item line.", Tags = ["Item Lines"], OperationId = "createItemLine", SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<ItemLine> request)
    {
        try
        {
            itemLineLogic.Add(request.Data);
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

    [Put("/item_lines/{id}")]
    [Describe("Updates an item line.", Tags = ["Item Lines"], OperationId = "updateItemLine")]
    public Response Update(Request<ItemLine> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return itemLineLogic.Update(id, request.Data) ? Response.Ok() : Response.NotFound();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Delete("/item_lines/{id}")]
    [Describe(
        "Deletes an item line.",
        Description = "Returns 409 while items still belong to the item line.",
        Tags = ["Item Lines"],
        OperationId = "deleteItemLine")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return itemLineLogic.Remove(id) ? Response.Ok() : Response.NotFound();
        }
        catch (InvalidOperationException)
        {
            return Response.Conflict();
        }
    }
}
