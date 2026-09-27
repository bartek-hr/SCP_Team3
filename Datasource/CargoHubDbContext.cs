using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Datasource;

public sealed class CargoHubDbContext(DbContextOptions<CargoHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}
