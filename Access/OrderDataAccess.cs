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
        Order? existing = context.Orders.Find(id);
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
}
