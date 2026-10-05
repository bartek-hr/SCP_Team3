using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class ItemTypeHandler(ItemTypeLogic logic)
{
    [Get("/item_types")]
    [Describe("Lists item types.", Tags = ["ItemTypes"])]
    public Response<IReadOnlyList<ItemType>> GetAll() => Response.Ok(logic.GetAll());

    [Get("/item_types/{id}")]
    [Describe("Gets an item type.", Tags = ["ItemTypes"])]
    public Response<ItemType> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<ItemType>.BadRequest();
        }

        ItemType? itemType = logic.GetById(id);
        return itemType is null ? Response<ItemType>.NotFound() : Response.Ok(itemType);
    }

    [Get("/item_types/{id}/items")]
    [Describe("Gets an item type's items.", Tags = ["ItemTypes"])]
    public Response<IReadOnlyList<int>> GetItems(Request request, ItemLogic itemLogic)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<int>>.BadRequest();
        }

        return logic.GetById(id) is null
            ? Response<IReadOnlyList<int>>.NotFound()
            : Response.Ok(itemLogic.GetIdsForItemType(id));
    }

    [Post("/item_types")]
    [Describe("Creates an item type.", Tags = ["ItemTypes"], SuccessStatus = 201)]
    public Response Add(Request<ItemType> request)
    {
        try
        {
            logic.Add(request.Data);
            return Response.Status(201);
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

    [Put("/item_types/{id}")]
    [Describe("Updates an item type.", Tags = ["ItemTypes"])]
    public Response Update(Request<ItemType> request)
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

    [Delete("/item_types/{id}")]
    [Describe("Deletes an item type.", Tags = ["ItemTypes"])]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        return logic.Remove(id) ? Response.Ok() : Response.NotFound();
    }
}
