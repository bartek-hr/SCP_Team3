using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class InventoryLogic(InventoryDataAccess dataAccess)
{
    public IReadOnlyList<Inventory> GetAll()
    {
        return dataAccess.GetAll();
    }

    public Inventory? GetByKey(int itemId, int locationId)
    {
        if (itemId <= 0 || locationId <= 0)
        {
            return null;
        }

        return dataAccess.GetByKey(itemId, locationId);
    }

    public IReadOnlyList<Inventory> GetByItemId(int itemId)
    {
        if (itemId <= 0)
        {
            return [];
        }

        return dataAccess.GetByItemId(itemId);
    }

    public InventoryTotals GetTotalsByItemId(int itemId)
    {
        IReadOnlyList<Inventory> inventories = GetByItemId(itemId);
        return new InventoryTotals
        {
            TotalExpected = inventories.Sum(inventory => (long)inventory.QuantityExpected),
            TotalOrdered = inventories.Sum(inventory => (long)inventory.QuantityOrdered),
            TotalAllocated = inventories.Sum(inventory => (long)inventory.QuantityAllocated),
            TotalAvailable = inventories.Sum(inventory =>
                (long)inventory.QuantityOnHand - inventory.QuantityAllocated)
        };
    }

    public void AddOrUpdate(Inventory inventory)
    {
        Validate(inventory);

        Inventory? existing = dataAccess.GetByKey(inventory.ItemId, inventory.LocationId);
        DateTime now = DateTime.UtcNow;
        if (existing is null)
        {
            inventory.CreatedAt = now;
        }
        else
        {
            inventory.CreatedAt = existing.CreatedAt;
        }
        inventory.UpdatedAt = now;
        dataAccess.AddOrUpdate(inventory);
    }

    public void Update(int itemId, int locationId, Inventory inventory)
    {
        ValidateKey(itemId, locationId);
        Validate(inventory);
        if (inventory.ItemId != itemId || inventory.LocationId != locationId)
        {
            throw new ArgumentException("The inventory item and location must match the key.", nameof(inventory));
        }

        Inventory? existing = dataAccess.GetByKey(itemId, locationId);
        if (existing is null)
        {
            return;
        }

        inventory.CreatedAt = existing.CreatedAt;
        inventory.UpdatedAt = DateTime.UtcNow;
        dataAccess.Update(itemId, locationId, inventory);
    }

    public void Remove(int itemId, int locationId)
    {
        ValidateKey(itemId, locationId);
        dataAccess.Remove(itemId, locationId);
    }

    private static void ValidateKey(int itemId, int locationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(locationId);
    }

    private static void Validate(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ValidateKey(inventory.ItemId, inventory.LocationId);
        ArgumentOutOfRangeException.ThrowIfNegative(inventory.QuantityOnHand);
        ArgumentOutOfRangeException.ThrowIfNegative(inventory.QuantityExpected);
        ArgumentOutOfRangeException.ThrowIfNegative(inventory.QuantityOrdered);
        ArgumentOutOfRangeException.ThrowIfNegative(inventory.QuantityAllocated);
    }
}
