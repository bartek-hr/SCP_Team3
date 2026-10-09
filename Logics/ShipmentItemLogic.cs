using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ShipmentItemLogic(ShipmentItemDataAccess dataAccess)
{

    public ShipmentItem? GetById(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return dataAccess.GetById(id);
    }

    public IReadOnlyList<ShipmentItem> GetItems(int shipmentId)
    {
        if (shipmentId <= 0)
        {
            return [];
        }

        return dataAccess.GetItems(shipmentId);
    }


    public void Add(ShipmentItem shipmentitem)
    {
        ValidateItem(shipmentitem);
        if (shipmentitem.Id > 0 && GetById(shipmentitem.Id) is not null)
        {
            throw new InvalidOperationException($"A shipment with ID {shipmentitem.Id} already exists.");
        }
        dataAccess.Add(shipmentitem);
    }

    public void Update(int id, ShipmentItem shipmentitem)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ValidateItem(shipmentitem);
        if (shipmentitem.Id != 0 && shipmentitem.Id != id)
        {
            throw new ArgumentException("The shipment ID must match the requested ID.", nameof(shipmentitem));
        }

        ShipmentItem? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        dataAccess.Update(id, shipmentitem);
    }

    public void Remove(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        dataAccess.Remove(id);
    }

    public static void ValidateItem(ShipmentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.ItemId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.Amount);
    }

    public static void ValidateItems(IReadOnlyList<ShipmentItem> items)
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
}
