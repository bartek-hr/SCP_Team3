using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class InventoryDataAccess(CargoHubDbContext context)
{
    private DbSet<Inventory> Inventories => context.Inventories;

    public IReadOnlyList<Inventory> GetAll() =>
        Inventories.AsNoTracking()
            .OrderBy(inventory => inventory.ItemId)
            .ThenBy(inventory => inventory.LocationId)
            .ToList();

    public Inventory? GetByKey(int itemId, int locationId) =>
        Inventories.AsNoTracking().SingleOrDefault(inventory =>
            inventory.ItemId == itemId && inventory.LocationId == locationId);

    public IReadOnlyList<Inventory> GetByItemId(int itemId) =>
        Inventories.AsNoTracking()
            .Where(inventory => inventory.ItemId == itemId)
            .OrderBy(inventory => inventory.LocationId)
            .ToList();

    public void AddOrUpdate(Inventory inventory)
    {
        Inventory? existing = Inventories.Find(inventory.ItemId, inventory.LocationId);
        if (existing is null)
        {
            Inventories.Add(inventory);
        }
        else
        {
            CopyValues(existing, inventory);
        }

        context.SaveChanges();
    }

    public void Update(int itemId, int locationId, Inventory inventory)
    {
        Inventory? existing = Inventories.Find(itemId, locationId);
        if (existing is null)
        {
            return;
        }

        CopyValues(existing, inventory);
        context.SaveChanges();
    }

    public void Remove(int itemId, int locationId)
    {
        Inventory? inventory = Inventories.Find(itemId, locationId);
        if (inventory is null)
        {
            return;
        }

        Inventories.Remove(inventory);
        context.SaveChanges();
    }

    private static void CopyValues(Inventory existing, Inventory inventory)
    {
        existing.QuantityOnHand = inventory.QuantityOnHand;
        existing.QuantityExpected = inventory.QuantityExpected;
        existing.QuantityOrdered = inventory.QuantityOrdered;
        existing.QuantityAllocated = inventory.QuantityAllocated;
        existing.UpdatedAt = inventory.UpdatedAt;
    }
}
