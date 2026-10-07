using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class OrderItemLogic(OrderItemDataAccess dataAccess, InventoryDataAccess inventoryAccess)
{
    public IReadOnlyList<OrderItem> GetByOrderId(int orderId)
    {
        if (orderId <= 0)
        {
            return [];
        }

        return dataAccess.GetByOrderId(orderId);
    }

    public void Replace(int orderId, IReadOnlyList<OrderItem> items)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);
        Validate(items);
        if (!dataAccess.OrderExists(orderId))
        {
            return;
        }

        // Net als in de Python-versie: verwerk het verschil per artikel op de locatie met de meeste voorraad.
        var previous = dataAccess.GetByOrderId(orderId).ToDictionary(item => item.ItemId, item => item.Amount);
        var current = items.ToDictionary(item => item.ItemId, item => item.Amount);
        foreach (int itemId in previous.Keys.Union(current.Keys))
        {
            int delta = current.GetValueOrDefault(itemId) - previous.GetValueOrDefault(itemId);
            if (delta != 0)
            {
                ApplyAllocatedDelta(itemId, delta);
            }
        }

        dataAccess.Replace(orderId, items, DateTime.UtcNow);
    }

    public static void Validate(IReadOnlyList<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var itemIds = new HashSet<int>();
        foreach (OrderItem item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.ItemId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.Amount);
            if (!itemIds.Add(item.ItemId))
                throw new ArgumentException("Each item may appear only once.", nameof(items));
            if (item.UnitPrice is < 0)
                throw new ArgumentOutOfRangeException(nameof(item.UnitPrice));
        }
    }

    private void ApplyAllocatedDelta(int itemId, int delta)
    {
        Inventory? inventory = inventoryAccess.GetByItemId(itemId)
            .OrderByDescending(row => row.QuantityOnHand).ThenBy(row => row.LocationId).FirstOrDefault();
        if (inventory is null)
        {
            return;
        }

        // De hoeveelheid wordt nooit negatief, net als in de Python-versie.
        inventory.QuantityAllocated = checked((int)Math.Max(0L, (long)inventory.QuantityAllocated + delta));
        inventory.UpdatedAt = DateTime.UtcNow;
        inventoryAccess.Update(inventory.ItemId, inventory.LocationId, inventory);
    }
}
