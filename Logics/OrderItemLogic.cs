using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class OrderItemLogic(OrderItemDataAccess dataAccess, OrderDataAccess orderAccess, ItemAccess itemAccess)
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

        EnsureItemNotInOrder(orderitem, 0);
        dataAccess.Add(orderitem);
    }

    public void Update(int id, OrderItem orderitem)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(orderitem);
        Validation.MatchesRoute(orderitem.Id, id);

        OrderItem? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        EnsureItemNotInOrder(orderitem, id);
        dataAccess.Update(id, orderitem);
    }


    public void Remove(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        dataAccess.Remove(id);
    }

    private void Validate(OrderItem orderitem)
    {
        ArgumentNullException.ThrowIfNull(orderitem);
        Validation.NotNegative(orderitem.Id, "id");
        Validation.Reference(orderitem.OrderId, "order_id", id => orderAccess.GetById(id) is not null);
        Validation.Reference(orderitem.ItemId, "item_id", id => itemAccess.GetById(id) is not null);
        Validation.Positive(orderitem.Amount, "amount");
        Validation.NotNegative(orderitem.UnitPrice, "unit_price");
    }

    // An order lists each item once, like the items of PUT /orders/{id}/items.
    private void EnsureItemNotInOrder(OrderItem orderitem, int ownId)
    {
        if (dataAccess.GetByOrderId(orderitem.OrderId).Any(row => row.ItemId == orderitem.ItemId && row.Id != ownId))
        {
            throw new InvalidOperationException($"Order {orderitem.OrderId} already contains item_id {orderitem.ItemId}.");
        }
    }
}
