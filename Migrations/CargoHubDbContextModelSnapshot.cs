using CargoHUB.Datasource;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace CargoHUB.Migrations;

[DbContext(typeof(CargoHubDbContext))]
internal partial class CargoHubDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");

        modelBuilder.Entity("CargoHUB.Models.Client", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasColumnName("id")
                    .HasJsonPropertyName("id");

                b.Property<string>("Address")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("address")
                    .HasJsonPropertyName("address");

                b.Property<string>("City")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("city")
                    .HasJsonPropertyName("city");

                b.Property<string>("ContactEmail")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("contact_email")
                    .HasJsonPropertyName("contact_email");

                b.Property<string>("ContactName")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("contact_name")
                    .HasJsonPropertyName("contact_name");

                b.Property<string>("ContactPhone")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("contact_phone")
                    .HasJsonPropertyName("contact_phone");

                b.Property<string>("Country")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("country")
                    .HasJsonPropertyName("country");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("created_at")
                    .HasJsonPropertyName("created_at");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("name")
                    .HasJsonPropertyName("name");

                b.Property<string>("Province")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("province")
                    .HasJsonPropertyName("province");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("updated_at")
                    .HasJsonPropertyName("updated_at");

                b.Property<string>("ZipCode")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("zip_code")
                    .HasJsonPropertyName("zip_code");

                b.HasKey("Id");

                b.ToTable("Clients", (string)null);
            });
#pragma warning restore 612, 618
    }
}
