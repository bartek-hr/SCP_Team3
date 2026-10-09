using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class OrderItemLogic(OrderItemDataAccess dataAccess)
{
    public IReadOnlyList<OrderItem> GetAll()
    {
        return dataAccess.GetAll();
    }

    public OrderItem? GetById(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return dataAccess.GetById(id);
    }

    public IReadOnlyList<OrderItem> GetItems(int orderId)
    {
        if (orderId <= 0)
        {
            return [];
        }

        return dataAccess.GetByOrderId(orderId);
    }



    public void Add(OrderItem orderitem)
    {
        Validate(orderitem);
        if (orderitem.Id > 0 && GetById(orderitem.Id) is not null)
        {
            throw new InvalidOperationException($"An orderitem with ID {orderitem.Id} already exists.");
        }


        dataAccess.Add(orderitem);
    }

    public void Update(int id, OrderItem orderitem)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(orderitem);
        if (orderitem.Id != 0 && orderitem.Id != id)
        {
            throw new ArgumentException("The order ID must match the requested ID.", nameof(orderitem));
        }

        OrderItem? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        dataAccess.Update(id, orderitem);
    }


    public void Remove(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        dataAccess.Remove(id);
    }

    private static void Validate(OrderItem orderitem)
    {
        ArgumentNullException.ThrowIfNull(orderitem);
        ArgumentOutOfRangeException.ThrowIfNegative(orderitem.Id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderitem.OrderId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderitem.ItemId);
        if (orderitem.UnitPrice is < 0)
            throw new ArgumentOutOfRangeException(nameof(orderitem.UnitPrice));
    }


}
