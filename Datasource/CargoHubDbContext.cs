using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Datasource;

public sealed class CargoHubDbContext(DbContextOptions<CargoHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferItem> TransferItems => Set<TransferItem>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventories");
            entity.HasKey(inventory => new { inventory.ItemId, inventory.LocationId });
            entity.Property(inventory => inventory.ItemId).HasColumnName("item_id");
            entity.Property(inventory => inventory.LocationId).HasColumnName("location_id");
            entity.Property(inventory => inventory.QuantityOnHand).HasColumnName("quantity_on_hand");
            entity.Property(inventory => inventory.QuantityExpected).HasColumnName("quantity_expected");
            entity.Property(inventory => inventory.QuantityOrdered).HasColumnName("quantity_ordered");
            entity.Property(inventory => inventory.QuantityAllocated).HasColumnName("quantity_allocated");
            entity.Property(inventory => inventory.CreatedAt).HasColumnName("created_at");
            entity.Property(inventory => inventory.UpdatedAt).HasColumnName("updated_at");
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
