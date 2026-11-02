using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class OrderLogic(
    OrderDataAccess dataAccess,
    ClientAccess clientAccess,
    WarehouseAccess warehouseAccess,
    ItemAccess itemAccess,
    ShipmentDataAccess shipmentAccess)
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

    public IReadOnlyList<OrderItem> GetItems(int orderId)
    {
        if (orderId <= 0)
        {
            return [];
        }

        return dataAccess.GetItems(orderId);
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

        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = order.CreatedAt;
        dataAccess.Add(order);
    }

    public void Update(int id, Order order)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(order);
        Validation.MatchesRoute(order.Id, id);

        Order? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

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
        if (shipmentAccess.AnyForOrder(id))
        {
            throw new InvalidOperationException($"Order {id} still has shipments.");
        }

        dataAccess.Remove(id);
    }

    private void Validate(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        Validation.NotNegative(order.Id, "id");
        Validation.Reference(order.ClientId, "client_id", ClientExists);
        Validation.Required(order.OrderDate, "order_date");
        Validation.Required(order.RequestDate, "request_date");
        if (order.RequestDate < order.OrderDate)
            throw new ArgumentException("request_date cannot be before order_date.");
        Validation.Required(order.Reference, "reference");
        Validation.Required(order.CustomerPoNumber, "customer_po_number");
        Validation.Required(order.OrderStatus, "order_status");
        Validation.Reference(order.WarehouseId, "warehouse_id", id => warehouseAccess.GetById(id) is not null);
        Validation.Reference(order.ShipToClientId, "ship_to_client_id", ClientExists);
        Validation.Reference(order.BillToClientId, "bill_to_client_id", ClientExists);
        ValidateItems(order.Items);
    }

    private void ValidateItems(IReadOnlyList<OrderItem> items)
    {
        Validation.Items(items, item => item.ItemId, item => item.Amount, id => itemAccess.GetById(id) is not null);
        for (int index = 0; index < items.Count; index++)
            Validation.NotNegative(items[index].UnitPrice, $"items[{index}].unit_price");
    }

    private bool ClientExists(int id) => clientAccess.GetById(id) is not null;
}
