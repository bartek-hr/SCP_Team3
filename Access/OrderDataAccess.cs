using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class OrderDataAccess(CargoHubDbContext context)
{
    public IReadOnlyList<Order> GetAll()
    {
        return context.Orders.AsNoTracking().Include(row => row.Items).OrderBy(row => row.Id).ToList();
    }

    public Order? GetById(int id)
    {
        return context.Orders.AsNoTracking().Include(row => row.Items).SingleOrDefault(row => row.Id == id);
    }

    public IReadOnlyList<OrderItem> GetItems(int id)
    {
        return context.Set<OrderItem>().AsNoTracking().Where(item => item.OrderId == id)
            .OrderBy(item => item.ItemId).ToList();
    }

    public IReadOnlyList<Order> GetByClientId(int clientId)
    {
        return context.Orders.AsNoTracking().Include(row => row.Items)
            .Where(row => row.ClientId == clientId || row.ShipToClientId == clientId || row.BillToClientId == clientId)
            .OrderBy(row => row.Id).ToList();
    }

    public void Add(Order order)
    {
        context.Orders.Add(order);
        context.SaveChanges();
    }

    public void Update(int id, Order order)
    {
        Order? existing = context.Orders.Include(row => row.Items).SingleOrDefault(row => row.Id == id);
        if (existing is null)
        {
            return;
        }

        existing.ClientId = order.ClientId;
        existing.OrderDate = order.OrderDate;
        existing.RequestDate = order.RequestDate;
        existing.Reference = order.Reference;
        existing.CustomerPoNumber = order.CustomerPoNumber;
        existing.OrderStatus = order.OrderStatus;
        existing.ShippingNotes = order.ShippingNotes;
        existing.WarehouseId = order.WarehouseId;
        existing.ShipToClientId = order.ShipToClientId;
        existing.BillToClientId = order.BillToClientId;
        existing.UpdatedAt = order.UpdatedAt;
        SetItems(existing, order.Items);
        context.SaveChanges();
    }

    public void ReplaceItems(int id, IReadOnlyList<OrderItem> items, DateTime updatedAt)
    {
        Order? existing = context.Orders.Include(row => row.Items).SingleOrDefault(row => row.Id == id);
        if (existing is null)
        {
            return;
        }

        SetItems(existing, items);
        existing.UpdatedAt = updatedAt;
        context.SaveChanges();
    }

    public void Remove(int id)
    {
        Order? existing = context.Orders.Find(id);
        if (existing is null)
        {
            return;
        }

        context.Orders.Remove(existing);
        context.SaveChanges();
    }

    private void SetItems(Order existing, IReadOnlyList<OrderItem> items)
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

            inventory.QuantityAllocated = checked((int)Math.Max(0L, (long)inventory.QuantityAllocated + delta));
            inventory.UpdatedAt = DateTime.UtcNow;
        }

        var itemIds = items.Select(item => item.ItemId).ToHashSet();
        existing.Items.RemoveAll(item => !itemIds.Contains(item.ItemId));
        foreach (OrderItem item in items)
        {
            OrderItem? stored = existing.Items.SingleOrDefault(row => row.ItemId == item.ItemId);
            if (stored is null)
            {
                stored = new OrderItem
                {
                    ItemId = item.ItemId
                };
                existing.Items.Add(stored);
            }

            stored.Amount = item.Amount;
            stored.UnitPrice = item.UnitPrice;
        }
    }
}
