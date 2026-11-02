using Microsoft.EntityFrameworkCore.Migrations;

namespace CargoHUB.Migrations;

public partial class CreateItemGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // IF NOT EXISTS: databases that ran CreateItems before it was split already have this table.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "ItemGroups" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_ItemGroups" PRIMARY KEY AUTOINCREMENT,
                "name" TEXT NOT NULL,
                "description" TEXT NOT NULL,
                "created_at" TEXT NOT NULL,
                "updated_at" TEXT NOT NULL
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ItemGroups");
    }
}
