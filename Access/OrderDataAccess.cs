using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class OrderDataAccess(CargoHubDbContext context)
{
    public IReadOnlyList<Order> GetAll() =>
        context.Orders.AsNoTracking().Include(row => row.Items).OrderBy(row => row.Id).ToList();

    public Order? GetById(int id) =>
        context.Orders.AsNoTracking().Include(row => row.Items).SingleOrDefault(row => row.Id == id);

    public IReadOnlyList<OrderItem> GetItems(int id) =>
        context.Set<OrderItem>().AsNoTracking().Where(item => item.OrderId == id)
            .OrderBy(item => item.ItemId).ToList();

    public IReadOnlyList<Order> GetByClientId(int clientId) =>
        context.Orders.AsNoTracking().Include(row => row.Items)
            .Where(row => row.ClientId == clientId || row.ShipToClientId == clientId || row.BillToClientId == clientId)
            .OrderBy(row => row.Id).ToList();

    public void Add(Order order)
    {
        context.Orders.Add(order);
        context.SaveChanges();
    }

    public void Update(int id, Order order)
    {
        Order? existing = context.Orders.Include(row => row.Items).SingleOrDefault(row => row.Id == id);
        if (existing is null)
            return;

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
            return;

        SetItems(existing, items);
        existing.UpdatedAt = updatedAt;
        context.SaveChanges();
    }

    public void Remove(int id)
    {
        Order? existing = context.Orders.Find(id);
        if (existing is null)
            return;

        context.Orders.Remove(existing);
        context.SaveChanges();
    }

    private static void SetItems(Order existing, IReadOnlyList<OrderItem> items)
    {
        var itemIds = items.Select(item => item.ItemId).ToHashSet();
        existing.Items.RemoveAll(item => !itemIds.Contains(item.ItemId));
        foreach (OrderItem item in items)
        {
            OrderItem? stored = existing.Items.SingleOrDefault(row => row.ItemId == item.ItemId);
            if (stored is null)
            {
                stored = new OrderItem { ItemId = item.ItemId };
                existing.Items.Add(stored);
            }

            stored.Amount = item.Amount;
            stored.UnitPrice = item.UnitPrice;
        }
    }
}
