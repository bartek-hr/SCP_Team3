using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ShipmentItemLogic(ShipmentItemDataAccess dataAccess, ShipmentDataAccess shipmentAccess, ItemAccess itemAccess)
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
        Validate(shipmentitem);
        if (shipmentitem.Id > 0 && GetById(shipmentitem.Id) is not null)
        {
            throw new InvalidOperationException($"A shipment item with ID {shipmentitem.Id} already exists.");
        }

        EnsureItemNotInShipment(shipmentitem, 0);
        dataAccess.Add(shipmentitem);
    }

    public void Update(int id, ShipmentItem shipmentitem)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(shipmentitem);
        Validation.MatchesRoute(shipmentitem.Id, id);

        ShipmentItem? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        EnsureItemNotInShipment(shipmentitem, id);
        dataAccess.Update(id, shipmentitem);
    }

    public void Remove(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        dataAccess.Remove(id);
    }

    private void Validate(ShipmentItem shipmentitem)
    {
        ArgumentNullException.ThrowIfNull(shipmentitem);
        Validation.NotNegative(shipmentitem.Id, "id");
        Validation.Reference(shipmentitem.ShipmentId, "shipment_id", id => shipmentAccess.GetById(id) is not null);
        Validation.Reference(shipmentitem.ItemId, "item_id", id => itemAccess.GetById(id) is not null);
        Validation.Positive(shipmentitem.Amount, "amount");
    }

    // A shipment lists each item once, like the items of PUT /shipments/{id}/items.
    private void EnsureItemNotInShipment(ShipmentItem shipmentitem, int ownId)
    {
        if (dataAccess.GetItems(shipmentitem.ShipmentId).Any(row => row.ItemId == shipmentitem.ItemId && row.Id != ownId))
        {
            throw new InvalidOperationException($"Shipment {shipmentitem.ShipmentId} already contains item_id {shipmentitem.ItemId}.");
        }
    }
}
