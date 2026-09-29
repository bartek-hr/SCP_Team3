using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class ItemGroupHandler(ItemGroupLogic itemGroupLogic, ItemLogic itemLogic)
{
    [Get("/item_groups")]
    [Describe("Lists item groups.", Tags = ["Item Groups"], OperationId = "getItemGroups")]
    public Response<IReadOnlyList<ItemGroup>> GetAll() => Response.Ok(itemGroupLogic.GetAll());

    [Get("/item_groups/{id}")]
    [Describe("Gets an item group.", Tags = ["Item Groups"], OperationId = "getItemGroup")]
    public Response<ItemGroup> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<ItemGroup>.BadRequest();
        }

        ItemGroup? itemGroup = itemGroupLogic.GetById(id);
        return itemGroup is null ? Response<ItemGroup>.NotFound() : Response.Ok(itemGroup);
    }

    [Get("/item_groups/{id}/items")]
    [Describe("Lists the IDs of the items in an item group.", Tags = ["Item Groups"], OperationId = "getItemGroupItems")]
    public Response<IReadOnlyList<int>> GetItems(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<int>>.BadRequest();
        }

        if (itemGroupLogic.GetById(id) is null)
        {
            return Response<IReadOnlyList<int>>.NotFound();
        }

        return Response.Ok(itemLogic.GetIdsForItemGroup(id));
    }

    [Post("/item_groups")]
    [Describe("Creates an item group.", Tags = ["Item Groups"], OperationId = "createItemGroup", SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<ItemGroup> request)
    {
        try
        {
            itemGroupLogic.Add(request.Data);
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

    [Put("/item_groups/{id}")]
    [Describe("Updates an item group.", Tags = ["Item Groups"], OperationId = "updateItemGroup")]
    public Response Update(Request<ItemGroup> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return itemGroupLogic.Update(id, request.Data) ? Response.Ok() : Response.NotFound();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Delete("/item_groups/{id}")]
    [Describe(
        "Deletes an item group.",
        Description = "Returns 409 while items still belong to the item group.",
        Tags = ["Item Groups"],
        OperationId = "deleteItemGroup")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return itemGroupLogic.Remove(id) ? Response.Ok() : Response.NotFound();
        }
        catch (InvalidOperationException)
        {
            return Response.Conflict();
        }
    }
}
