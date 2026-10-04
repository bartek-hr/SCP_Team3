using Microsoft.EntityFrameworkCore.Migrations;

namespace CargoHUB.Migrations;

public partial class ZendingenImport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
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
            CREATE TABLE ShipmentItems (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                shipment_id INTEGER NOT NULL REFERENCES Shipments(id) ON DELETE CASCADE,
                item_id INTEGER NOT NULL,
                amount INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX IX_ShipmentItems_shipment_id_item_id ON ShipmentItems(shipment_id, item_id);
            """);
        LegacyJsonImport.Import(migrationBuilder, "Shipments", "shipment");
        LegacyJsonImport.Import(migrationBuilder, "ShipmentItems", "shipment_item");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ShipmentItems");
        migrationBuilder.DropTable("Shipments");
    }
}
