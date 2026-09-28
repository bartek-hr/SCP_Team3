using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ShipmentLogic(ShipmentDataAccess dataAccess)
{
    public IReadOnlyList<Shipment> GetAll()
    {
        return dataAccess.GetAll();
    }

    public Shipment? GetById(int id)
    {
        return id > 0 ? dataAccess.GetById(id) : null;
    }

    public IReadOnlyList<ShipmentItem> GetItems(int shipmentId)
    {
        return shipmentId > 0 ? dataAccess.GetItems(shipmentId) : [];
    }

    public IReadOnlyList<int> GetOrderIds(int shipmentId)
    {
        return GetById(shipmentId)?.OrderId is int orderId ? [orderId] : [];
    }

    public void Add(Shipment shipment)
    {
        Validate(shipment);
        if (shipment.Id > 0 && GetById(shipment.Id) is not null)
            throw new InvalidOperationException($"A shipment with ID {shipment.Id} already exists.");

        shipment.CreatedAt = DateTime.UtcNow;
        shipment.UpdatedAt = shipment.CreatedAt;
        dataAccess.Add(shipment);
    }

    public void Update(int id, Shipment shipment)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(shipment);
        if (shipment.Id != 0 && shipment.Id != id)
            throw new ArgumentException("The shipment ID must match the requested ID.", nameof(shipment));

        Shipment? existing = GetById(id);
        if (existing is null)
            return;

        shipment.CreatedAt = existing.CreatedAt;
        shipment.UpdatedAt = DateTime.UtcNow;
        dataAccess.Update(id, shipment);
    }

    public void ReplaceItems(int shipmentId, IReadOnlyList<ShipmentItem> items)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(shipmentId);
        ValidateItems(items);
        dataAccess.ReplaceItems(shipmentId, items, DateTime.UtcNow);
    }

    public void ReplaceOrders(int shipmentId, IReadOnlyList<int> orderIds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(shipmentId);
        ArgumentNullException.ThrowIfNull(orderIds);
        foreach (int orderId in orderIds)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);

        // The legacy API accepts a list but stores only the first order ID.
        dataAccess.ReplaceOrders(shipmentId, orderIds.Count > 0 ? orderIds[0] : null, DateTime.UtcNow);
    }

    public void Remove(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        dataAccess.Remove(id);
    }

    private static void Validate(Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentOutOfRangeException.ThrowIfNegative(shipment.Id);
        if (string.IsNullOrWhiteSpace(shipment.Reference))
            throw new ArgumentException("Reference is required.", nameof(shipment.Reference));
        if (shipment.OrderId is int orderId)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);
        if (shipment.ShipmentDate == default)
            throw new ArgumentException("ShipmentDate is required.", nameof(shipment.ShipmentDate));
        if (string.IsNullOrWhiteSpace(shipment.ShipmentType))
            throw new ArgumentException("ShipmentType is required.", nameof(shipment.ShipmentType));
        if (string.IsNullOrWhiteSpace(shipment.ShipmentStatus))
            throw new ArgumentException("ShipmentStatus is required.", nameof(shipment.ShipmentStatus));
        if (string.IsNullOrWhiteSpace(shipment.CarrierName))
            throw new ArgumentException("CarrierName is required.", nameof(shipment.CarrierName));
        if (string.IsNullOrWhiteSpace(shipment.ShippingMethod))
            throw new ArgumentException("ShippingMethod is required.", nameof(shipment.ShippingMethod));
        if (string.IsNullOrWhiteSpace(shipment.PaymentType))
            throw new ArgumentException("PaymentType is required.", nameof(shipment.PaymentType));
        ValidateItems(shipment.Items);
    }

    private static void ValidateItems(IReadOnlyList<ShipmentItem> items)
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
