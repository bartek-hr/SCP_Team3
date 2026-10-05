using Microsoft.EntityFrameworkCore.Migrations;

namespace CargoHUB.Migrations;

public partial class OrdersImport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
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
            CREATE TABLE OrderItems (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                order_id INTEGER NOT NULL REFERENCES Orders(id) ON DELETE CASCADE,
                item_id INTEGER NOT NULL,
                amount INTEGER NOT NULL,
                unit_price TEXT
            );
            CREATE UNIQUE INDEX IX_OrderItems_order_id_item_id ON OrderItems(order_id, item_id);
            """);
        LegacyJsonImport.Import(migrationBuilder, "Orders", "order");
        LegacyJsonImport.Import(migrationBuilder, "OrderItems", "order_item");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("OrderItems");
        migrationBuilder.DropTable("Orders");
    }
}
