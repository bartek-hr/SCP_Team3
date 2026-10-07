using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ShipmentLogic(ShipmentDataAccess dataAccess, ShipmentItemLogic itemLogic)
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

        // Net als in de Python-versie past een nieuwe shipment de voorraad niet aan.
        shipment.Items ??= [];
        shipment.CreatedAt = DateTime.UtcNow;
        shipment.UpdatedAt = shipment.CreatedAt;
        dataAccess.Add(shipment);
    }

    public void Update(int id, Shipment shipment)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(shipment);
        if (shipment.Id != 0 && shipment.Id != id)
        {
            throw new ArgumentException("The shipment ID must match the requested ID.", nameof(shipment));
        }

        Shipment? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        shipment.CreatedAt = existing.CreatedAt;
        shipment.UpdatedAt = DateTime.UtcNow;
        dataAccess.Update(id, shipment);
        if (shipment.Items is not null)
        {
            itemLogic.Replace(id, shipment.Items);
        }
    }

    public void ReplaceOrders(int shipmentId, IReadOnlyList<int> orderIds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(shipmentId);
        ArgumentNullException.ThrowIfNull(orderIds);
        foreach (int orderId in orderIds)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);
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
        if (shipment.Items is not null)
            ShipmentItemLogic.Validate(shipment.Items);
    }
}
