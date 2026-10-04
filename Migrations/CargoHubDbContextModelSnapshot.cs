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

        modelBuilder.Entity("CargoHUB.Models.Inventory", b =>
            {
                b.Property<int>("ItemId")
                    .HasColumnType("INTEGER")
                    .HasJsonPropertyName("item_id");

                b.Property<int>("LocationId")
                    .HasColumnType("INTEGER")
                    .HasJsonPropertyName("location_id");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT")
                    .HasJsonPropertyName("created_at");

                b.Property<int>("QuantityAllocated")
                    .HasColumnType("INTEGER")
                    .HasJsonPropertyName("quantity_allocated");

                b.Property<int>("QuantityExpected")
                    .HasColumnType("INTEGER")
                    .HasJsonPropertyName("quantity_expected");

                b.Property<int>("QuantityOnHand")
                    .HasColumnType("INTEGER")
                    .HasJsonPropertyName("quantity_on_hand");

                b.Property<int>("QuantityOrdered")
                    .HasColumnType("INTEGER")
                    .HasJsonPropertyName("quantity_ordered");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("TEXT")
                    .HasJsonPropertyName("updated_at");

                b.HasKey("ItemId", "LocationId");

                b.ToTable("Inventories");
            });

        modelBuilder.Entity("CargoHUB.Models.Location", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasColumnName("id")
                    .HasJsonPropertyName("id");

                b.Property<string>("Code")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("code")
                    .HasJsonPropertyName("code");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("created_at")
                    .HasJsonPropertyName("created_at");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("name")
                    .HasJsonPropertyName("name");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("updated_at")
                    .HasJsonPropertyName("updated_at");

                b.Property<int>("WarehouseId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("warehouse_id")
                    .HasJsonPropertyName("warehouse_id");

                b.HasKey("Id");

                b.HasIndex("WarehouseId");

                b.ToTable("Locations", (string)null);
            });

        modelBuilder.Entity("CargoHUB.Models.Order", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasColumnName("id")
                    .HasJsonPropertyName("id");

                b.Property<int>("BillToClientId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("bill_to_client_id")
                    .HasJsonPropertyName("bill_to_client_id");

                b.Property<int>("ClientId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("client_id")
                    .HasJsonPropertyName("client_id");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("created_at")
                    .HasJsonPropertyName("created_at");

                b.Property<string>("CustomerPoNumber")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("customer_po_number")
                    .HasJsonPropertyName("customer_po_number");

                b.Property<DateTime>("OrderDate")
                    .HasColumnType("TEXT")
                    .HasColumnName("order_date")
                    .HasJsonPropertyName("order_date");

                b.Property<string>("OrderStatus")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("order_status")
                    .HasJsonPropertyName("order_status");

                b.Property<string>("Reference")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("reference")
                    .HasJsonPropertyName("reference");

                b.Property<DateTime>("RequestDate")
                    .HasColumnType("TEXT")
                    .HasColumnName("request_date")
                    .HasJsonPropertyName("request_date");

                b.Property<int>("ShipToClientId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("ship_to_client_id")
                    .HasJsonPropertyName("ship_to_client_id");

                b.Property<string>("ShippingNotes")
                    .HasColumnType("TEXT")
                    .HasColumnName("shipping_notes")
                    .HasJsonPropertyName("shipping_notes");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("updated_at")
                    .HasJsonPropertyName("updated_at");

                b.Property<int>("WarehouseId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("warehouse_id")
                    .HasJsonPropertyName("warehouse_id");

                b.HasKey("Id");

                b.ToTable("Orders", (string)null);
            });

        modelBuilder.Entity("CargoHUB.Models.OrderItem", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasColumnName("id");

                b.Property<int>("Amount")
                    .HasColumnType("INTEGER")
                    .HasColumnName("amount")
                    .HasJsonPropertyName("amount");

                b.Property<int>("ItemId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("item_id")
                    .HasJsonPropertyName("item_id");

                b.Property<int>("OrderId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("order_id");

                b.Property<decimal?>("UnitPrice")
                    .HasColumnType("TEXT")
                    .HasColumnName("unit_price")
                    .HasJsonPropertyName("unit_price");

                b.HasKey("Id");

                b.HasIndex("OrderId", "ItemId")
                    .IsUnique();

                b.ToTable("OrderItems", (string)null);

                b.HasAnnotation("Relational:JsonPropertyName", "items");
            });

        modelBuilder.Entity("CargoHUB.Models.Shipment", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasColumnName("id")
                    .HasJsonPropertyName("id");

                b.Property<string>("CarrierName")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("carrier_name")
                    .HasJsonPropertyName("carrier_name");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("created_at")
                    .HasJsonPropertyName("created_at");

                b.Property<int?>("OrderId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("order_id")
                    .HasJsonPropertyName("order_id");

                b.Property<string>("PaymentType")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("payment_type")
                    .HasJsonPropertyName("payment_type");

                b.Property<string>("Reference")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("reference")
                    .HasJsonPropertyName("reference");

                b.Property<DateTime>("ShipmentDate")
                    .HasColumnType("TEXT")
                    .HasColumnName("shipment_date")
                    .HasJsonPropertyName("shipment_date");

                b.Property<string>("ShipmentStatus")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("shipment_status")
                    .HasJsonPropertyName("shipment_status");

                b.Property<string>("ShipmentType")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("shipment_type")
                    .HasJsonPropertyName("shipment_type");

                b.Property<string>("ShippingMethod")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("shipping_method")
                    .HasJsonPropertyName("shipping_method");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("updated_at")
                    .HasJsonPropertyName("updated_at");

                b.HasKey("Id");

                b.ToTable("Shipments", (string)null);
            });

        modelBuilder.Entity("CargoHUB.Models.ShipmentItem", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasColumnName("id");

                b.Property<int>("Amount")
                    .HasColumnType("INTEGER")
                    .HasColumnName("amount")
                    .HasJsonPropertyName("amount");

                b.Property<int>("ItemId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("item_id")
                    .HasJsonPropertyName("item_id");

                b.Property<int>("ShipmentId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("shipment_id");

                b.HasKey("Id");

                b.HasIndex("ShipmentId", "ItemId")
                    .IsUnique();

                b.ToTable("ShipmentItems", (string)null);

                b.HasAnnotation("Relational:JsonPropertyName", "items");
            });

        modelBuilder.Entity("CargoHUB.Models.Transfer", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasColumnName("id")
                    .HasJsonPropertyName("id");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("created_at")
                    .HasJsonPropertyName("created_at");

                b.Property<int>("FromLocationId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("from_location_id")
                    .HasJsonPropertyName("from_location_id");

                b.Property<string>("Reference")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("reference")
                    .HasJsonPropertyName("reference");

                b.Property<int>("ToLocationId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("to_location_id")
                    .HasJsonPropertyName("to_location_id");

                b.Property<string>("TransferStatus")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("transfer_status")
                    .HasJsonPropertyName("transfer_status");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("TEXT")
                    .HasColumnName("updated_at")
                    .HasJsonPropertyName("updated_at");

                b.HasKey("Id");

                b.HasIndex("FromLocationId");

                b.HasIndex("ToLocationId");

                b.ToTable("Transfers", (string)null);
            });

        modelBuilder.Entity("CargoHUB.Models.TransferItem", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER")
                    .HasColumnName("id");

                b.Property<int>("Amount")
                    .HasColumnType("INTEGER")
                    .HasColumnName("amount")
                    .HasJsonPropertyName("amount");

                b.Property<int>("ItemId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("item_id")
                    .HasJsonPropertyName("item_id");

                b.Property<int>("TransferId")
                    .HasColumnType("INTEGER")
                    .HasColumnName("transfer_id");

                b.HasKey("Id");

                b.HasIndex("TransferId");

                b.ToTable("TransferItems", (string)null);

                b.HasAnnotation("Relational:JsonPropertyName", "items");
            });

        modelBuilder.Entity("CargoHUB.Models.Warehouse", b =>
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

                b.Property<string>("Code")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("code")
                    .HasJsonPropertyName("code");

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

                b.ToTable("Warehouses", (string)null);
            });

        modelBuilder.Entity("CargoHUB.Models.Location", b =>
            {
                b.HasOne("CargoHUB.Models.Warehouse", null)
                    .WithMany()
                    .HasForeignKey("WarehouseId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
            });

        modelBuilder.Entity("CargoHUB.Models.OrderItem", b =>
            {
                b.HasOne("CargoHUB.Models.Order", null)
                    .WithMany("Items")
                    .HasForeignKey("OrderId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

        modelBuilder.Entity("CargoHUB.Models.ShipmentItem", b =>
            {
                b.HasOne("CargoHUB.Models.Shipment", null)
                    .WithMany("Items")
                    .HasForeignKey("ShipmentId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

        modelBuilder.Entity("CargoHUB.Models.Transfer", b =>
            {
                b.HasOne("CargoHUB.Models.Location", null)
                    .WithMany()
                    .HasForeignKey("FromLocationId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("CargoHUB.Models.Location", null)
                    .WithMany()
                    .HasForeignKey("ToLocationId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
            });

        modelBuilder.Entity("CargoHUB.Models.TransferItem", b =>
            {
                b.HasOne("CargoHUB.Models.Transfer", null)
                    .WithMany("Items")
                    .HasForeignKey("TransferId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

        modelBuilder.Entity("CargoHUB.Models.Order", b =>
            {
                b.Navigation("Items");
            });

        modelBuilder.Entity("CargoHUB.Models.Shipment", b =>
            {
                b.Navigation("Items");
            });

        modelBuilder.Entity("CargoHUB.Models.Transfer", b =>
            {
                b.Navigation("Items");
            });
#pragma warning restore 612, 618
    }
}
