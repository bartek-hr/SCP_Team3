using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class ShipmentDataAccess(CargoHubDbContext context)
{
    public IReadOnlyList<Shipment> GetAll()
    {
        return context.Shipments.AsNoTracking().Include(row => row.Items).OrderBy(row => row.Id).ToList();
    }

    public Shipment? GetById(int id)
    {
        return context.Shipments.AsNoTracking().Include(row => row.Items).SingleOrDefault(row => row.Id == id);
    }

    public void Add(Shipment shipment)
    {
        context.Shipments.Add(shipment);
        context.SaveChanges();
    }

    public void Update(int id, Shipment shipment)
    {
        Shipment? existing = context.Shipments.Find(id);
        if (existing is null)
            return;

        existing.Reference = shipment.Reference;
        existing.OrderId = shipment.OrderId;
        existing.ShipmentDate = shipment.ShipmentDate;
        existing.ShipmentType = shipment.ShipmentType;
        existing.ShipmentStatus = shipment.ShipmentStatus;
        existing.CarrierName = shipment.CarrierName;
        existing.ShippingMethod = shipment.ShippingMethod;
        existing.PaymentType = shipment.PaymentType;
        existing.UpdatedAt = shipment.UpdatedAt;
        context.SaveChanges();
    }

    public void ReplaceOrders(int id, int? orderId, DateTime updatedAt)
    {
        Shipment? existing = context.Shipments.Find(id);
        if (existing is null)
            return;

        existing.OrderId = orderId;
        existing.UpdatedAt = updatedAt;
        context.SaveChanges();
    }

    public void Remove(int id)
    {
        Shipment? existing = context.Shipments.Find(id);
        if (existing is null)
            return;

        context.Shipments.Remove(existing);
        context.SaveChanges();
    }
}
