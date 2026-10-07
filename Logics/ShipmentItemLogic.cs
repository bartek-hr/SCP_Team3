using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ShipmentItemLogic(ShipmentItemDataAccess dataAccess, InventoryDataAccess inventoryAccess)
{
    public IReadOnlyList<ShipmentItem> GetByShipmentId(int shipmentId)
    {
        if (shipmentId <= 0)
        {
            return [];
        }

        return dataAccess.GetByShipmentId(shipmentId);
    }

    public void Replace(int shipmentId, IReadOnlyList<ShipmentItem> items)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(shipmentId);
        Validate(items);
        if (!dataAccess.ShipmentExists(shipmentId))
        {
            return;
        }

        // Net als in de Python-versie: verwerk het verschil per artikel op de locatie met de meeste voorraad.
        var previous = dataAccess.GetByShipmentId(shipmentId).ToDictionary(item => item.ItemId, item => item.Amount);
        var current = items.ToDictionary(item => item.ItemId, item => item.Amount);
        foreach (int itemId in previous.Keys.Union(current.Keys))
        {
            int delta = current.GetValueOrDefault(itemId) - previous.GetValueOrDefault(itemId);
            if (delta != 0)
            {
                ApplyOrderedDelta(itemId, delta);
            }
        }

        dataAccess.Replace(shipmentId, items, DateTime.UtcNow);
    }

    public static void Validate(IReadOnlyList<ShipmentItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var itemIds = new HashSet<int>();
        foreach (ShipmentItem item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.ItemId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.Amount);
            if (!itemIds.Add(item.ItemId))
                throw new ArgumentException("Each item may appear only once.", nameof(items));
        }
    }

    private void ApplyOrderedDelta(int itemId, int delta)
    {
        Inventory? inventory = inventoryAccess.GetByItemId(itemId)
            .OrderByDescending(row => row.QuantityOnHand).ThenBy(row => row.LocationId).FirstOrDefault();
        if (inventory is null)
        {
            return;
        }

        // De hoeveelheid wordt nooit negatief, net als in de Python-versie.
        inventory.QuantityOrdered = checked((int)Math.Max(0L, (long)inventory.QuantityOrdered + delta));
        inventory.UpdatedAt = DateTime.UtcNow;
        inventoryAccess.Update(inventory.ItemId, inventory.LocationId, inventory);
    }
}
