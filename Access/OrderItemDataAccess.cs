using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class OrderItemDataAccess(CargoHubDbContext context)
{
    public IReadOnlyList<OrderItem> GetByOrderId(int orderId)
    {
        return context.OrderItems.AsNoTracking().Where(item => item.OrderId == orderId)
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

        context.OrderItems.RemoveRange(context.OrderItems.Where(item => item.OrderId == orderId));
        context.OrderItems.AddRange(items.Select(item => new OrderItem
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
