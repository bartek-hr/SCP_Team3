using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Datasource;

public sealed class CargoHubDbContext(DbContextOptions<CargoHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

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
    }
}
