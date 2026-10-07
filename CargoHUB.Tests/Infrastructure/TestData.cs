using CargoHUB.Models;

namespace CargoHUB.Tests.Infrastructure;

internal static class TestData
{
    public static readonly DateTime CreatedAt = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedAt = new(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);

    public static Warehouse CreateWarehouse(int id = 0, string name = "Test Warehouse") => new()
    {
        Id = id,
        Code = "TST-WH",
        Name = name,
        Address = "Teststraat 1",
        City = "Rotterdam",
        ZipCode = "3011 AB",
        Province = "Zuid-Holland",
        Country = "Netherlands",
        ContactName = "Test Persoon",
        ContactPhone = "(010) 1234567",
        ContactEmail = "test-wh@example.com",
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
    };

    public static Location CreateLocation(int id = 0, int warehouseId = 1, string name = "Zone A Aisle 1 Rack 1 Bin 1") => new()
    {
        Id = id,
        WarehouseId = warehouseId,
        Code = "TST-A01-R1-B1",
        Name = name,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
    };

    public static Transfer CreateTransfer(int id = 0, int fromLocationId = 1, int toLocationId = 2, params TransferItem[] items) => new()
    {
        Id = id,
        Reference = "TRF-TEST",
        FromLocationId = fromLocationId,
        ToLocationId = toLocationId,
        TransferStatus = TransferStatus.Scheduled,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
        Items = [.. items],
    };

    public static TransferItem CreateItem(int itemId, int amount) => new()
    {
        ItemId = itemId,
        Amount = amount,
    };

    public static Order CreateOrder(int id = 0, params OrderItem[] items) => new()
    {
        Id = id,
        ClientId = 1,
        OrderDate = CreatedAt,
        RequestDate = UpdatedAt,
        Reference = "ORD-TEST",
        CustomerPoNumber = "PO-TEST",
        OrderStatus = "Pending",
        WarehouseId = 1,
        ShipToClientId = 1,
        BillToClientId = 1,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
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
        ShipmentDate = CreatedAt,
        ShipmentType = "I",
        ShipmentStatus = "Pending",
        CarrierName = "DPD",
        ShippingMethod = "Fastest",
        PaymentType = "Manual",
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
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
