using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations
{
    /// <inheritdoc />
    public partial class VoorraadOrdersEnZendingen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityExpected = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityOrdered = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityAllocated = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => new { x.ItemId, x.LocationId });
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_id = table.Column<int>(type: "INTEGER", nullable: false),
                    order_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    request_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    reference = table.Column<string>(type: "TEXT", nullable: false),
                    customer_po_number = table.Column<string>(type: "TEXT", nullable: false),
                    order_status = table.Column<string>(type: "TEXT", nullable: false),
                    shipping_notes = table.Column<string>(type: "TEXT", nullable: true),
                    warehouse_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ship_to_client_id = table.Column<int>(type: "INTEGER", nullable: false),
                    bill_to_client_id = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    reference = table.Column<string>(type: "TEXT", nullable: false),
                    order_id = table.Column<int>(type: "INTEGER", nullable: true),
                    shipment_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    shipment_type = table.Column<string>(type: "TEXT", nullable: false),
                    shipment_status = table.Column<string>(type: "TEXT", nullable: false),
                    carrier_name = table.Column<string>(type: "TEXT", nullable: false),
                    shipping_method = table.Column<string>(type: "TEXT", nullable: false),
                    payment_type = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    order_id = table.Column<int>(type: "INTEGER", nullable: false),
                    item_id = table.Column<int>(type: "INTEGER", nullable: false),
                    amount = table.Column<int>(type: "INTEGER", nullable: false),
                    unit_price = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_order_id",
                        column: x => x.order_id,
                        principalTable: "Orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentItems",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    shipment_id = table.Column<int>(type: "INTEGER", nullable: false),
                    item_id = table.Column<int>(type: "INTEGER", nullable: false),
                    amount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentItems", x => x.id);
                    table.ForeignKey(
                        name: "FK_ShipmentItems_Shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "Shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_order_id_item_id",
                table: "OrderItems",
                columns: new[] { "order_id", "item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentItems_shipment_id_item_id",
                table: "ShipmentItems",
                columns: new[] { "shipment_id", "item_id" },
                unique: true);

            ImporteerJson(migrationBuilder, "Inventories", "inventory",
                [
                    ("ItemId", typeof(int)),
                    ("LocationId", typeof(int)),
                    ("QuantityOnHand", typeof(int)),
                    ("QuantityExpected", typeof(int)),
                    ("QuantityOrdered", typeof(int)),
                    ("QuantityAllocated", typeof(int)),
                    ("CreatedAt", typeof(DateTime)),
                    ("UpdatedAt", typeof(DateTime))
                ]);

            ImporteerJson(migrationBuilder, "Orders", "order",
                [
                    ("id", typeof(int)),
                    ("client_id", typeof(int)),
                    ("order_date", typeof(DateTime)),
                    ("request_date", typeof(DateTime)),
                    ("reference", typeof(string)),
                    ("customer_po_number", typeof(string)),
                    ("order_status", typeof(string)),
                    ("shipping_notes", typeof(string)),
                    ("warehouse_id", typeof(int)),
                    ("ship_to_client_id", typeof(int)),
                    ("bill_to_client_id", typeof(int)),
                    ("created_at", typeof(DateTime)),
                    ("updated_at", typeof(DateTime))
                ]);

            ImporteerJson(migrationBuilder, "OrderItems", "order_item",
                [
                    ("id", typeof(int)),
                    ("order_id", typeof(int)),
                    ("item_id", typeof(int)),
                    ("amount", typeof(int)),
                    ("unit_price", typeof(decimal))
                ]);

            ImporteerJson(migrationBuilder, "Shipments", "shipment",
                [
                    ("id", typeof(int)),
                    ("reference", typeof(string)),
                    ("order_id", typeof(int)),
                    ("shipment_date", typeof(DateTime)),
                    ("shipment_type", typeof(string)),
                    ("shipment_status", typeof(string)),
                    ("carrier_name", typeof(string)),
                    ("shipping_method", typeof(string)),
                    ("payment_type", typeof(string)),
                    ("created_at", typeof(DateTime)),
                    ("updated_at", typeof(DateTime))
                ]);

            ImporteerJson(migrationBuilder, "ShipmentItems", "shipment_item",
                [
                    ("id", typeof(int)),
                    ("shipment_id", typeof(int)),
                    ("item_id", typeof(int)),
                    ("amount", typeof(int))
                ]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "ShipmentItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Shipments");
        }

        // Lees rechtstreeks uit JSON zodat ook de verborgen item- en ouder-ID's behouden blijven.
        private static void ImporteerJson(MigrationBuilder migrationBuilder, string table, string file,
            (string Name, Type Type)[] columns)
        {
            using Stream stream = typeof(VoorraadOrdersEnZendingen).Assembly
                .GetManifestResourceStream($"CargoHUB.LegacyData.{file}Json");
            if (stream is null)
            {
                throw new InvalidOperationException($"Legacybestand {file}.json ontbreekt.");
            }
            using JsonDocument document = JsonDocument.Parse(stream);
            var values = new object[document.RootElement.GetArrayLength(), columns.Length];
            int index = 0;
            foreach (JsonElement row in document.RootElement.EnumerateArray())
            {
                for (int column = 0; column < columns.Length; column++)
                {
                    JsonElement value = row.GetProperty(JsonNamingPolicy.SnakeCaseLower.ConvertName(columns[column].Name));
                    if (value.ValueKind == JsonValueKind.Null)
                    {
                        values[index, column] = null;
                    }
                    else
                    {
                        values[index, column] = value.Deserialize(columns[column].Type);
                    }
                }
                index++;
            }

            migrationBuilder.InsertData(table, columns.Select(column => column.Name).ToArray(), values);
        }
    }
}
