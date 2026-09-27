using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Datasource;

public sealed class CargoHubDbContext(DbContextOptions<CargoHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}
