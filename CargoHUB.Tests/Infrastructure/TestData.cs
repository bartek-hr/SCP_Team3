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

    public static ItemLine CreateItemLine(int id = 0, string name = "Tech Gadgets") => new()
    {
        Id = id,
        Name = name,
        Description = "Test item line",
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
    };

    public static ItemGroup CreateItemGroup(int id = 0, string name = "Electronics") => new()
    {
        Id = id,
        Name = name,
        Description = "Test item group",
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
    };

    public static Item CreateCatalogItem(int id = 0, int itemLineId = 1, int itemGroupId = 1, string code = "TST-ITEM") => new()
    {
        Id = id,
        Code = code,
        Description = "Test item",
        Barcode = "0012345678905",
        ModelNumber = "TM-01",
        CommodityCode = 1234,
        UnitWeight = 1.5m,
        ItemLineId = itemLineId,
        ItemGroupId = itemGroupId,
        ItemTypeId = 1,
        MinPurchaseQty = 1,
        CaseSize = 10,
        PackagingType = "Box",
        OrderMultiple = 1,
        SupplierId = 1,
        SupplierSku = "SUP-SKU-1",
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
    };
}
