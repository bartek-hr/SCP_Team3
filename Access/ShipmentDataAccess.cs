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

    public IReadOnlyList<ShipmentItem> GetItems(int id)
    {
        return context.Set<ShipmentItem>().AsNoTracking().Where(item => item.ShipmentId == id)
            .OrderBy(item => item.ItemId).ToList();
    }

    public void Add(Shipment shipment)
    {
        context.Shipments.Add(shipment);
        context.SaveChanges();
    }

    public void Update(int id, Shipment shipment)
    {
        Shipment? existing = context.Shipments.Include(row => row.Items).SingleOrDefault(row => row.Id == id);
        if (existing is null)
        {
            return;
        }

        existing.Reference = shipment.Reference;
        existing.OrderId = shipment.OrderId;
        existing.ShipmentDate = shipment.ShipmentDate;
        existing.ShipmentType = shipment.ShipmentType;
        existing.ShipmentStatus = shipment.ShipmentStatus;
        existing.CarrierName = shipment.CarrierName;
        existing.ShippingMethod = shipment.ShippingMethod;
        existing.PaymentType = shipment.PaymentType;
        existing.UpdatedAt = shipment.UpdatedAt;
        SetItems(existing, shipment.Items);
        context.SaveChanges();
    }

    public void ReplaceItems(int id, IReadOnlyList<ShipmentItem> items, DateTime updatedAt)
    {
        Shipment? existing = context.Shipments.Include(row => row.Items).SingleOrDefault(row => row.Id == id);
        if (existing is null)
        {
            return;
        }

        SetItems(existing, items);
        existing.UpdatedAt = updatedAt;
        context.SaveChanges();
    }

    public void ReplaceOrders(int id, int? orderId, DateTime updatedAt)
    {
        Shipment? existing = context.Shipments.Find(id);
        if (existing is null)
        {
            return;
        }

        existing.OrderId = orderId;
        existing.UpdatedAt = updatedAt;
        context.SaveChanges();
    }

    public void Remove(int id)
    {
        Shipment? existing = context.Shipments.Find(id);
        if (existing is null)
        {
            return;
        }

        context.Shipments.Remove(existing);
        context.SaveChanges();
    }

    private void SetItems(Shipment existing, IReadOnlyList<ShipmentItem> items)
    {
        // Net als in de Python-versie: verwerk het verschil op de locatie met de meeste voorraad.
        var previous = existing.Items.ToDictionary(item => item.ItemId, item => item.Amount);
        var current = items.ToDictionary(item => item.ItemId, item => item.Amount);
        foreach (int itemId in previous.Keys.Union(current.Keys))
        {
            int delta = current.GetValueOrDefault(itemId) - previous.GetValueOrDefault(itemId);
            if (delta == 0)
            {
                continue;
            }

            Inventory? inventory = context.Inventories.Where(row => row.ItemId == itemId)
                .OrderByDescending(row => row.QuantityOnHand).ThenBy(row => row.LocationId).FirstOrDefault();
            if (inventory is null)
            {
                continue;
            }

            inventory.QuantityOrdered = checked((int)Math.Max(0L, (long)inventory.QuantityOrdered + delta));
            inventory.UpdatedAt = DateTime.UtcNow;
        }

        var itemIds = items.Select(item => item.ItemId).ToHashSet();
        existing.Items.RemoveAll(item => !itemIds.Contains(item.ItemId));
        foreach (ShipmentItem item in items)
        {
            ShipmentItem? stored = existing.Items.SingleOrDefault(row => row.ItemId == item.ItemId);
            if (stored is null)
            {
                stored = new ShipmentItem
                {
                    ItemId = item.ItemId
                };
                existing.Items.Add(stored);
            }

            stored.Amount = item.Amount;
        }
    }
}
