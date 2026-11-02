using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ShipmentLogic(ShipmentDataAccess dataAccess, OrderDataAccess orderAccess, ItemAccess itemAccess)
{
    public IReadOnlyList<Shipment> GetAll()
    {
        return dataAccess.GetAll();
    }

    public Shipment? GetById(int id)
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

    public IReadOnlyList<int> GetOrderIds(int shipmentId)
    {
        Shipment? shipment = GetById(shipmentId);
        if (shipment is null || shipment.OrderId is null)
        {
            return [];
        }

        return [shipment.OrderId.Value];
    }

    public void Add(Shipment shipment)
    {
        Validate(shipment);
        if (shipment.Id > 0 && GetById(shipment.Id) is not null)
        {
            throw new InvalidOperationException($"A shipment with ID {shipment.Id} already exists.");
        }

        shipment.CreatedAt = DateTime.UtcNow;
        shipment.UpdatedAt = shipment.CreatedAt;
        dataAccess.Add(shipment);
    }

    public void Update(int id, Shipment shipment)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(shipment);
        Validation.MatchesRoute(shipment.Id, id);

        Shipment? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

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
        {
            Validation.Reference(orderId, "order_id", OrderExists);
        }

        // The legacy API accepts a list but stores only the first order ID.
        int? firstOrderId = null;
        if (orderIds.Count > 0)
        {
            firstOrderId = orderIds[0];
        }

        dataAccess.ReplaceOrders(shipmentId, firstOrderId, DateTime.UtcNow);
    }

    public void Remove(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        dataAccess.Remove(id);
    }

    private void Validate(Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        Validation.NotNegative(shipment.Id, "id");
        Validation.Required(shipment.Reference, "reference");
        if (shipment.OrderId is int orderId)
            Validation.Reference(orderId, "order_id", OrderExists);
        Validation.Required(shipment.ShipmentDate, "shipment_date");
        Validation.Required(shipment.ShipmentType, "shipment_type");
        Validation.Required(shipment.ShipmentStatus, "shipment_status");
        Validation.Required(shipment.CarrierName, "carrier_name");
        Validation.Required(shipment.ShippingMethod, "shipping_method");
        Validation.Required(shipment.PaymentType, "payment_type");
        ValidateItems(shipment.Items);
    }

    private void ValidateItems(IReadOnlyList<ShipmentItem> items)
    {
        Validation.Items(items, item => item.ItemId, item => item.Amount, id => itemAccess.GetById(id) is not null);
    }

    private bool OrderExists(int id) => orderAccess.GetById(id) is not null;
}
