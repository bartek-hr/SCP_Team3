using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class InventoryHandler(InventoryLogic logic)
{
    [Get("/inventories")]
    [Describe("Geeft alle voorraadregels terug.", Tags = ["Inventories"], OperationId = "getInventories")]
    public Response<IReadOnlyList<Inventory>> GetAll() => Response.Ok(logic.GetAll());

    [Get("/inventories/{itemId}/{locationId}")]
    [Describe("Geeft de voorraad van een artikel op een locatie terug.", Tags = ["Inventories"], OperationId = "getInventory")]
    public Response<Inventory> GetByKey(Request request)
    {
        if (!TryKey(request, out int itemId, out int locationId))
            return Response<Inventory>.BadRequest();

        Inventory? inventory = logic.GetByKey(itemId, locationId);
        return inventory is null ? Response<Inventory>.NotFound() : Response.Ok(inventory);
    }

    [Get("/items/{id}/inventory")]
    [Describe("Geeft de voorraadregels van een artikel terug.", Tags = ["Inventories"], OperationId = "getItemInventory")]
    public Response<IReadOnlyList<Inventory>> GetByItemId(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
            return Response<IReadOnlyList<Inventory>>.BadRequest();

        return Response.Ok(logic.GetByItemId(id));
    }

    [Get("/items/{id}/inventory/totals")]
    [Describe("Geeft de voorraadtotalen van een artikel terug.", Tags = ["Inventories"], OperationId = "getItemInventoryTotals")]
    public Response<InventoryTotals> GetTotalsByItemId(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
            return Response<InventoryTotals>.BadRequest();

        return Response.Ok(logic.GetTotalsByItemId(id));
    }

    [Post("/inventories")]
    [Describe("Voegt voorraad toe of vervangt de bestaande voorraadregel.", Tags = ["Inventories"], OperationId = "upsertInventory", SuccessStatus = StatusCodes.Status201Created)]
    public Response AddOrUpdate(Request<Inventory> request)
    {
        try
        {
            logic.AddOrUpdate(request.Data);
            return Response.Created();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Put("/inventories/{itemId}/{locationId}")]
    [Describe("Wijzigt een voorraadregel.", Tags = ["Inventories"], OperationId = "updateInventory")]
    public Response Update(Request<Inventory> request)
    {
        if (!TryKey(request, out int itemId, out int locationId))
            return Response.BadRequest();
        if (logic.GetByKey(itemId, locationId) is null)
            return Response.NotFound();

        try
        {
            logic.Update(itemId, locationId, request.Data);
            return Response.Ok();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Delete("/inventories/{itemId}/{locationId}")]
    [Describe("Verwijdert een voorraadregel.", Tags = ["Inventories"], OperationId = "deleteInventory")]
    public Response Remove(Request request)
    {
        if (!TryKey(request, out int itemId, out int locationId))
            return Response.BadRequest();
        if (logic.GetByKey(itemId, locationId) is null)
            return Response.NotFound();

        logic.Remove(itemId, locationId);
        return Response.Ok();
    }

    private static bool TryKey(Request request, out int itemId, out int locationId)
    {
        bool validItem = request.TryParam<int>("itemId", out itemId) && itemId > 0;
        bool validLocation = request.TryParam<int>("locationId", out locationId) && locationId > 0;
        return validItem && validLocation;
    }
}
