using CargoHUB.Models;

namespace CargoHUB.Tests.Infrastructure;

public static class OrderShipmentTestData
{
    public static Order CreateOrder(int id = 0, params OrderItem[] items) => new()
    {
        Id = id,
        ClientId = 1,
        OrderDate = TestData.CreatedAt,
        RequestDate = TestData.UpdatedAt,
        Reference = "ORD-TEST",
        CustomerPoNumber = "PO-TEST",
        OrderStatus = "Pending",
        WarehouseId = 1,
        ShipToClientId = 1,
        BillToClientId = 1,
        CreatedAt = TestData.CreatedAt,
        UpdatedAt = TestData.UpdatedAt,
        Items = [.. items],
    };

    public static OrderItem CreateOrderItem(int itemId, int amount, decimal? unitPrice = null) => new()
    {
        ItemId = itemId,
        Amount = amount,
        UnitPrice = unitPrice,
    };

    public static Shipment CreateShipment(int id = 0, params ShipmentItem[] items) => new()
    {
        Id = id,
        Reference = "SHP-TEST",
        ShipmentDate = TestData.CreatedAt,
        ShipmentType = "I",
        ShipmentStatus = "Pending",
        CarrierName = "DPD",
        ShippingMethod = "Fastest",
        PaymentType = "Manual",
        CreatedAt = TestData.CreatedAt,
        UpdatedAt = TestData.UpdatedAt,
        Items = [.. items],
    };

    public static ShipmentItem CreateShipmentItem(int itemId, int amount) => new()
    {
        ItemId = itemId,
        Amount = amount,
    };

    // Two locations hold item 1; location 2 has the most stock, so stock rules land there.
    public static Inventory[] CreateInventoriesForItem1() =>
    [
        new()
        {
            ItemId = 1,
            LocationId = 1,
            QuantityOnHand = 5,
            QuantityAllocated = 1,
        },
        new()
        {
            ItemId = 1,
            LocationId = 2,
            QuantityOnHand = 10,
            QuantityAllocated = 2,
            QuantityOrdered = 2,
        },
    ];
}
