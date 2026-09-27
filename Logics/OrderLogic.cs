using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class OrderLogic(OrderDataAccess dataAccess)
{
    public IReadOnlyList<Order> GetAll() => dataAccess.GetAll();

    public Order? GetById(int id) => id > 0 ? dataAccess.GetById(id) : null;

    public IReadOnlyList<OrderItem> GetItems(int orderId) =>
        orderId > 0 ? dataAccess.GetItems(orderId) : [];

    public IReadOnlyList<Order> GetByClientId(int clientId) =>
        clientId > 0 ? dataAccess.GetByClientId(clientId) : [];

    public void Add(Order order)
    {
        Validate(order);
        if (order.Id > 0 && GetById(order.Id) is not null)
            throw new InvalidOperationException($"An order with ID {order.Id} already exists.");

        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = order.CreatedAt;
        dataAccess.Add(order);
    }

    public void Update(int id, Order order)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(order);
        if (order.Id != 0 && order.Id != id)
            throw new ArgumentException("The order ID must match the requested ID.", nameof(order));

        Order? existing = GetById(id);
        if (existing is null)
            return;

        order.CreatedAt = existing.CreatedAt;
        order.UpdatedAt = DateTime.UtcNow;
        dataAccess.Update(id, order);
    }

    public void ReplaceItems(int orderId, IReadOnlyList<OrderItem> items)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(orderId);
        ValidateItems(items);
        dataAccess.ReplaceItems(orderId, items, DateTime.UtcNow);
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
        ValidateItems(order.Items);
    }

    private static void ValidateItems(IReadOnlyList<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var itemIds = new HashSet<int>();
        foreach (OrderItem item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.ItemId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.Amount);
            if (!itemIds.Add(item.ItemId))
                throw new ArgumentException("Each item may appear only once.", nameof(items));
            if (item.UnitPrice is < 0)
                throw new ArgumentOutOfRangeException(nameof(item.UnitPrice));
        }
    }
}
