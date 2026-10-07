using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class OrderLogic(OrderDataAccess dataAccess, OrderItemLogic itemLogic)
{
    public IReadOnlyList<Order> GetAll()
    {
        return dataAccess.GetAll();
    }

    public Order? GetById(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return dataAccess.GetById(id);
    }

    public IReadOnlyList<Order> GetByClientId(int clientId)
    {
        if (clientId <= 0)
        {
            return [];
        }

        return dataAccess.GetByClientId(clientId);
    }

    public void Add(Order order)
    {
        Validate(order);
        if (order.Id > 0 && GetById(order.Id) is not null)
        {
            throw new InvalidOperationException($"An order with ID {order.Id} already exists.");
        }

        // Net als in de Python-versie past een nieuwe order de voorraad niet aan.
        order.Items ??= [];
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = order.CreatedAt;
        dataAccess.Add(order);
    }

    public void Update(int id, Order order)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(order);
        if (order.Id != 0 && order.Id != id)
        {
            throw new ArgumentException("The order ID must match the requested ID.", nameof(order));
        }

        Order? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        order.CreatedAt = existing.CreatedAt;
        order.UpdatedAt = DateTime.UtcNow;
        dataAccess.Update(id, order);
        if (order.Items is not null)
        {
            itemLogic.Replace(id, order.Items);
        }
    }

    public void Remove(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        dataAccess.Remove(id);
    }

    private static void Validate(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentOutOfRangeException.ThrowIfNegative(order.Id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order.ClientId);
        if (order.OrderDate == default)
            throw new ArgumentException("OrderDate is required.", nameof(order.OrderDate));
        if (order.RequestDate == default)
            throw new ArgumentException("RequestDate is required.", nameof(order.RequestDate));
        if (string.IsNullOrWhiteSpace(order.Reference))
            throw new ArgumentException("Reference is required.", nameof(order.Reference));
        if (string.IsNullOrWhiteSpace(order.CustomerPoNumber))
            throw new ArgumentException("CustomerPoNumber is required.", nameof(order.CustomerPoNumber));
        if (string.IsNullOrWhiteSpace(order.OrderStatus))
            throw new ArgumentException("OrderStatus is required.", nameof(order.OrderStatus));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order.WarehouseId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order.ShipToClientId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order.BillToClientId);
        if (order.Items is not null)
            OrderItemLogic.Validate(order.Items);
    }
}
