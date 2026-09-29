using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class ItemHandler(ItemLogic itemLogic)
{
    [Get("/items")]
    [Describe("Lists items.", Tags = ["Items"], OperationId = "getItems")]
    public Response<IReadOnlyList<Item>> GetAll() => Response.Ok(itemLogic.GetAll());

    [Get("/items/{id}")]
    [Describe("Gets an item.", Tags = ["Items"], OperationId = "getItem")]
    public Response<Item> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<Item>.BadRequest();
        }

        Item? item = itemLogic.GetById(id);
        return item is null ? Response<Item>.NotFound() : Response.Ok(item);
    }

    [Get("/items/{id}/inventory")]
    [Describe("Lists the inventory for an item.", Tags = ["Items"], OperationId = "getItemInventory")]
    public Response GetInventory()
    {
        return Response.NotImplemented();
    }

    [Get("/items/{id}/inventory/totals")]
    [Describe("Gets the inventory totals for an item.", Tags = ["Items"], OperationId = "getItemInventoryTotals")]
    public Response GetInventoryTotals()
    {
        return Response.NotImplemented();
    }

    [Post("/items")]
    [Describe("Creates an item.", Tags = ["Items"], OperationId = "createItem", SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<Item> request)
    {
        try
        {
            itemLogic.Add(request.Data);
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

    [Put("/items/{id}")]
    [Describe("Updates an item.", Tags = ["Items"], OperationId = "updateItem")]
    public Response Update(Request<Item> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return itemLogic.Update(id, request.Data) ? Response.Ok() : Response.NotFound();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Delete("/items/{id}")]
    [Describe("Deletes an item.", Tags = ["Items"], OperationId = "deleteItem")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        return itemLogic.Remove(id) ? Response.Ok() : Response.NotFound();
    }
}
