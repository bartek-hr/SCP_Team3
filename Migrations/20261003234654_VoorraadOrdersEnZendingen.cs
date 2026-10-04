using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations;

public partial class VoorraadOrdersEnZendingen : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE Inventories (
                ItemId INTEGER NOT NULL,
                LocationId INTEGER NOT NULL,
                QuantityOnHand INTEGER NOT NULL,
                QuantityExpected INTEGER NOT NULL,
                QuantityOrdered INTEGER NOT NULL,
                QuantityAllocated INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                PRIMARY KEY (ItemId, LocationId)
            );
            CREATE TABLE Orders (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                client_id INTEGER NOT NULL,
                order_date TEXT NOT NULL,
                request_date TEXT NOT NULL,
                reference TEXT NOT NULL,
                customer_po_number TEXT NOT NULL,
                order_status TEXT NOT NULL,
                shipping_notes TEXT,
                warehouse_id INTEGER NOT NULL,
                ship_to_client_id INTEGER NOT NULL,
                bill_to_client_id INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            CREATE TABLE Shipments (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                reference TEXT NOT NULL,
                order_id INTEGER,
                shipment_date TEXT NOT NULL,
                shipment_type TEXT NOT NULL,
                shipment_status TEXT NOT NULL,
                carrier_name TEXT NOT NULL,
                shipping_method TEXT NOT NULL,
                payment_type TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            CREATE TABLE OrderItems (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                order_id INTEGER NOT NULL REFERENCES Orders(id) ON DELETE CASCADE,
                item_id INTEGER NOT NULL,
                amount INTEGER NOT NULL,
                unit_price TEXT
            );
            CREATE TABLE ShipmentItems (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                shipment_id INTEGER NOT NULL REFERENCES Shipments(id) ON DELETE CASCADE,
                item_id INTEGER NOT NULL,
                amount INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX IX_OrderItems_order_id_item_id ON OrderItems(order_id, item_id);
            CREATE UNIQUE INDEX IX_ShipmentItems_shipment_id_item_id ON ShipmentItems(shipment_id, item_id);
            """);

        ImporteerJson(migrationBuilder, "Inventories", "inventory");
        ImporteerJson(migrationBuilder, "Orders", "order");
        ImporteerJson(migrationBuilder, "OrderItems", "order_item");
        ImporteerJson(migrationBuilder, "Shipments", "shipment");
        ImporteerJson(migrationBuilder, "ShipmentItems", "shipment_item");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE ShipmentItems;
            DROP TABLE OrderItems;
            DROP TABLE Shipments;
            DROP TABLE Orders;
            DROP TABLE Inventories;
            """);
    }

    private static void ImporteerJson(MigrationBuilder migrationBuilder, string table, string file)
    {
        using Stream stream = typeof(VoorraadOrdersEnZendingen).Assembly
            .GetManifestResourceStream($"CargoHUB.LegacyData.{file}Json");
        if (stream is null)
        {
            throw new InvalidOperationException($"Legacybestand {file}.json ontbreekt.");
        }

        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement rows = document.RootElement;
        string[] names = rows[0].EnumerateObject().Select(property => property.Name).ToArray();
        string[] columns = names.ToArray();
        if (table == "Inventories")
        {
            columns = names.Select(name => string.Concat(name.Split('_')
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)))).ToArray();
        }

        var values = new object[rows.GetArrayLength(), names.Length];
        for (int row = 0; row < rows.GetArrayLength(); row++)
        {
            for (int column = 0; column < names.Length; column++)
            {
                JsonElement value = rows[row].GetProperty(names[column]);
                if (value.ValueKind == JsonValueKind.Number && names[column] == "unit_price")
                {
                    values[row, column] = value.GetDecimal();
                }
                else if (value.ValueKind == JsonValueKind.Number)
                {
                    values[row, column] = value.GetInt32();
                }
                else if (value.ValueKind == JsonValueKind.String)
                {
                    values[row, column] = value.GetString();
                    if (names[column].EndsWith("_at") || names[column].EndsWith("_date"))
                    {
                        values[row, column] = value.GetDateTime();
                    }
                }
            }
        }

        migrationBuilder.InsertData(table, columns, values);
    }
}
