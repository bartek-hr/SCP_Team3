using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class OrderItemDataAccess(CargoHubDbContext context)
{
    public IReadOnlyList<OrderItem> GetAll() =>
        context.OrderItems.AsNoTracking().OrderBy(OrderItem => OrderItem.Id).ToList();

    public OrderItem? GetById(int id) =>
        context.OrderItems.AsNoTracking().SingleOrDefault(OrderItem => OrderItem.Id == id);

    public IReadOnlyList<OrderItem> GetByOrderId(int id) {
        return context.OrderItems.AsNoTracking()
            .Where(row => row.OrderId == id) 
            .OrderBy(row => row.Id).ToList();     
    } 

    public void Add(OrderItem OrderItem)
    {
        context.OrderItems.Add(OrderItem);
        context.SaveChanges();
    }

    public bool Update(int id, OrderItem OrderItem)
    {
        OrderItem? existing = context.OrderItems.Find(id);
        if (existing is null)
        {
            return false;
        }

        existing.OrderId = OrderItem.OrderId;
        existing.ItemId = OrderItem.ItemId;
        existing.Amount = OrderItem.Amount;
        existing.UnitPrice = OrderItem.UnitPrice;
        context.SaveChanges();
        return true;
    }

    public bool Remove(int id)
    {
        OrderItem? OrderItem = context.OrderItems.Find(id);
        if (OrderItem is null)
        {
            return false;
        }

        context.OrderItems.Remove(OrderItem);
        context.SaveChanges();
        return true;
    }
}
