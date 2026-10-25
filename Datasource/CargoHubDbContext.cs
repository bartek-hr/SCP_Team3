using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Datasource;

public sealed class CargoHubDbContext(DbContextOptions<CargoHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ItemType> ItemTypes => Set<ItemType>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferItem> TransferItems => Set<TransferItem>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<ItemLine> ItemLines => Set<ItemLine>();
    public DbSet<ItemGroup> ItemGroups => Set<ItemGroup>();
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.ClientId).HasColumnName("client_id");
            entity.Property(row => row.OrderDate).HasColumnName("order_date");
            entity.Property(row => row.RequestDate).HasColumnName("request_date");
            entity.Property(row => row.Reference).HasColumnName("reference");
            entity.Property(row => row.CustomerPoNumber).HasColumnName("customer_po_number");
            entity.Property(row => row.OrderStatus).HasColumnName("order_status");
            entity.Property(row => row.ShippingNotes).HasColumnName("shipping_notes");
            entity.Property(row => row.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(row => row.ShipToClientId).HasColumnName("ship_to_client_id");
            entity.Property(row => row.BillToClientId).HasColumnName("bill_to_client_id");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
            entity.Property(row => row.UpdatedAt).HasColumnName("updated_at");
            entity.HasMany(row => row.Items).WithOne().HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OrderId, item.ItemId }).IsUnique();
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.OrderId).HasColumnName("order_id");
            entity.Property(item => item.ItemId).HasColumnName("item_id");
            entity.Property(item => item.Amount).HasColumnName("amount");
            entity.Property(item => item.UnitPrice).HasColumnName("unit_price");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("Shipments");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.Reference).HasColumnName("reference");
            entity.Property(row => row.OrderId).HasColumnName("order_id");
            entity.Property(row => row.ShipmentDate).HasColumnName("shipment_date");
            entity.Property(row => row.ShipmentType).HasColumnName("shipment_type");
            entity.Property(row => row.ShipmentStatus).HasColumnName("shipment_status");
            entity.Property(row => row.CarrierName).HasColumnName("carrier_name");
            entity.Property(row => row.ShippingMethod).HasColumnName("shipping_method");
            entity.Property(row => row.PaymentType).HasColumnName("payment_type");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
            entity.Property(row => row.UpdatedAt).HasColumnName("updated_at");
            entity.HasMany(row => row.Items).WithOne().HasForeignKey(item => item.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShipmentItem>(entity =>
        {
            entity.ToTable("ShipmentItems");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ShipmentId, item.ItemId }).IsUnique();
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.ShipmentId).HasColumnName("shipment_id");
            entity.Property(item => item.ItemId).HasColumnName("item_id");
            entity.Property(item => item.Amount).HasColumnName("amount");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasKey(client => client.Id);
            entity.Property(client => client.Id).HasColumnName("id");
            entity.Property(client => client.Name).HasColumnName("name").IsRequired();
            entity.Property(client => client.Address).HasColumnName("address").IsRequired();
            entity.Property(client => client.City).HasColumnName("city").IsRequired();
            entity.Property(client => client.ZipCode).HasColumnName("zip_code").IsRequired();
            entity.Property(client => client.Province).HasColumnName("province").IsRequired();
            entity.Property(client => client.Country).HasColumnName("country").IsRequired();
            entity.Property(client => client.ContactName).HasColumnName("contact_name").IsRequired();
            entity.Property(client => client.ContactPhone).HasColumnName("contact_phone").IsRequired();
            entity.Property(client => client.ContactEmail).HasColumnName("contact_email").IsRequired();
            entity.Property(client => client.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(client => client.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.HasKey(supplier => supplier.Id);
            entity.Property(supplier => supplier.Id).HasColumnName("id");
            entity.Property(supplier => supplier.Code).HasColumnName("code").IsRequired();
            entity.Property(supplier => supplier.Name).HasColumnName("name").IsRequired();
            entity.Property(supplier => supplier.Address).HasColumnName("address").IsRequired();
            entity.Property(supplier => supplier.City).HasColumnName("city").IsRequired();
            entity.Property(supplier => supplier.ZipCode).HasColumnName("zip_code").IsRequired();
            entity.Property(supplier => supplier.Province).HasColumnName("province").IsRequired();
            entity.Property(supplier => supplier.Country).HasColumnName("country").IsRequired();
            entity.Property(supplier => supplier.ContactName).HasColumnName("contact_name").IsRequired();
            entity.Property(supplier => supplier.PhoneNumber).HasColumnName("phone_number").IsRequired();
            entity.Property(supplier => supplier.Reference).HasColumnName("reference").IsRequired();
            entity.Property(supplier => supplier.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(supplier => supplier.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<ItemType>(entity =>
        {
            entity.ToTable("ItemTypes");
            entity.HasKey(itemType => itemType.Id);
            entity.Property(itemType => itemType.Id).HasColumnName("id");
            entity.Property(itemType => itemType.Name).HasColumnName("name").IsRequired();
            entity.Property(itemType => itemType.Description).HasColumnName("description").IsRequired();
            entity.Property(itemType => itemType.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(itemType => itemType.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<ItemLine>(entity =>
        {
            entity.ToTable("ItemLines");
            entity.HasKey(itemLine => itemLine.Id);
            entity.Property(itemLine => itemLine.Id).HasColumnName("id");
            entity.Property(itemLine => itemLine.Name).HasColumnName("name").IsRequired();
            entity.Property(itemLine => itemLine.Description).HasColumnName("description");
            entity.Property(itemLine => itemLine.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(itemLine => itemLine.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<ItemGroup>(entity =>
        {
            entity.ToTable("ItemGroups");
            entity.HasKey(itemGroup => itemGroup.Id);
            entity.Property(itemGroup => itemGroup.Id).HasColumnName("id");
            entity.Property(itemGroup => itemGroup.Name).HasColumnName("name").IsRequired();
            entity.Property(itemGroup => itemGroup.Description).HasColumnName("description");
            entity.Property(itemGroup => itemGroup.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(itemGroup => itemGroup.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Code).HasColumnName("code").IsRequired();
            entity.Property(item => item.Description).HasColumnName("description").IsRequired();
            entity.Property(item => item.Barcode).HasColumnName("barcode");
            entity.Property(item => item.ModelNumber).HasColumnName("model_number");
            entity.Property(item => item.CommodityCode).HasColumnName("commodity_code");
            entity.Property(item => item.UnitWeight).HasColumnName("unit_weight");
            entity.Property(item => item.ItemLineId).HasColumnName("item_line_id").IsRequired();
            entity.Property(item => item.ItemGroupId).HasColumnName("item_group_id").IsRequired();
            entity.Property(item => item.ItemTypeId).HasColumnName("item_type_id");
            entity.Property(item => item.MinPurchaseQty).HasColumnName("min_purchase_qty");
            entity.Property(item => item.CaseSize).HasColumnName("case_size");
            entity.Property(item => item.PackagingType).HasColumnName("packaging_type");
            entity.Property(item => item.OrderMultiple).HasColumnName("order_multiple");
            entity.Property(item => item.SupplierId).HasColumnName("supplier_id");
            entity.Property(item => item.SupplierSku).HasColumnName("supplier_sku");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasOne<ItemLine>()
                .WithMany()
                .HasForeignKey(item => item.ItemLineId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ItemGroup>()
                .WithMany()
                .HasForeignKey(item => item.ItemGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Warehouses");
            entity.HasKey(warehouse => warehouse.Id);
            entity.Property(warehouse => warehouse.Id).HasColumnName("id");
            entity.Property(warehouse => warehouse.Code).HasColumnName("code").IsRequired();
            entity.Property(warehouse => warehouse.Name).HasColumnName("name").IsRequired();
            entity.Property(warehouse => warehouse.Address).HasColumnName("address").IsRequired();
            entity.Property(warehouse => warehouse.City).HasColumnName("city").IsRequired();
            entity.Property(warehouse => warehouse.ZipCode).HasColumnName("zip_code").IsRequired();
            entity.Property(warehouse => warehouse.Province).HasColumnName("province").IsRequired();
            entity.Property(warehouse => warehouse.Country).HasColumnName("country").IsRequired();
            entity.Property(warehouse => warehouse.ContactName).HasColumnName("contact_name").IsRequired();
            entity.Property(warehouse => warehouse.ContactPhone).HasColumnName("contact_phone").IsRequired();
            entity.Property(warehouse => warehouse.ContactEmail).HasColumnName("contact_email").IsRequired();
            entity.Property(warehouse => warehouse.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(warehouse => warehouse.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("Locations");
            entity.HasKey(location => location.Id);
            entity.Property(location => location.Id).HasColumnName("id");
            entity.Property(location => location.WarehouseId).HasColumnName("warehouse_id").IsRequired();
            entity.Property(location => location.Code).HasColumnName("code").IsRequired();
            entity.Property(location => location.Name).HasColumnName("name").IsRequired();
            entity.Property(location => location.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(location => location.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(location => location.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.ToTable("Transfers");
            entity.HasKey(transfer => transfer.Id);
            entity.Property(transfer => transfer.Id).HasColumnName("id");
            entity.Property(transfer => transfer.Reference).HasColumnName("reference").IsRequired();
            entity.Property(transfer => transfer.FromLocationId).HasColumnName("from_location_id").IsRequired();
            entity.Property(transfer => transfer.ToLocationId).HasColumnName("to_location_id").IsRequired();
            entity.Property(transfer => transfer.TransferStatus).HasColumnName("transfer_status").HasConversion<string>().IsRequired();
            entity.Property(transfer => transfer.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(transfer => transfer.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasOne<Location>()
                .WithMany()
                .HasForeignKey(transfer => transfer.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Location>()
                .WithMany()
                .HasForeignKey(transfer => transfer.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(transfer => transfer.Items)
                .WithOne()
                .HasForeignKey(item => item.TransferId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TransferItem>(entity =>
        {
            entity.ToTable("TransferItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.TransferId).HasColumnName("transfer_id").IsRequired();
            entity.Property(item => item.ItemId).HasColumnName("item_id").IsRequired();
            entity.Property(item => item.Amount).HasColumnName("amount").IsRequired();
        });
    }
}
