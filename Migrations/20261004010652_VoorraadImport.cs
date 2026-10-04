using Microsoft.EntityFrameworkCore.Migrations;

namespace CargoHUB.Migrations;

public partial class VoorraadImport : Migration
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
            """);
        LegacyJsonImport.Import(migrationBuilder, "Inventories", "inventory");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Inventories");
    }
}
