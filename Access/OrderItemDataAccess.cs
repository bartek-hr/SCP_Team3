using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class OrderItemDataAccess(CargoHubDbContext context)
{
    public IReadOnlyList<OrderItem> GetByOrderId(int orderId)
    {
        return context.Set<OrderItem>().AsNoTracking().Where(item => item.OrderId == orderId)
            .OrderBy(item => item.ItemId).ToList();
    }

    public bool OrderExists(int orderId)
    {
        return context.Orders.Any(row => row.Id == orderId);
    }

    public void Replace(int orderId, IReadOnlyList<OrderItem> items, DateTime updatedAt)
    {
        Order? order = context.Orders.Find(orderId);
        if (order is null)
        {
            return;
        }

        context.Set<OrderItem>().RemoveRange(context.Set<OrderItem>().Where(item => item.OrderId == orderId));
        context.Set<OrderItem>().AddRange(items.Select(item => new OrderItem
        {
            OrderId = orderId,
            ItemId = item.ItemId,
            Amount = item.Amount,
            UnitPrice = item.UnitPrice,
        }));
        order.UpdatedAt = updatedAt;
        context.SaveChanges();
    }
}
