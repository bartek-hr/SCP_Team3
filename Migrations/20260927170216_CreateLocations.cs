using System.Text.Json;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations;

public partial class CreateLocations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Locations",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                warehouse_id = table.Column<int>(type: "INTEGER", nullable: false),
                code = table.Column<string>(type: "TEXT", nullable: false),
                name = table.Column<string>(type: "TEXT", nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Locations", x => x.id);
                table.ForeignKey(
                    name: "FK_Locations_Warehouses_warehouse_id",
                    column: x => x.warehouse_id,
                    principalTable: "Warehouses",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Locations_warehouse_id",
            table: "Locations",
            column: "warehouse_id");

        migrationBuilder.InsertData(
            table: "Locations",
            columns: ["id", "warehouse_id", "code", "name", "created_at", "updated_at"],
            values: GetLocationSeedValues());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Locations");
    }

    private static object[,] GetLocationSeedValues()
    {
        using Stream stream = typeof(CreateLocations).Assembly.GetManifestResourceStream("CargoHUB.LegacyData.locationJson")
                           ?? throw new InvalidOperationException("The legacy location data resource is missing.");
        List<Location> locations = JsonSerializer.Deserialize<List<Location>>(stream)
                                   ?? throw new InvalidOperationException("The legacy location data is invalid.");
        object[,] values = new object[locations.Count, 6];

        for (int index = 0; index < locations.Count; index++)
        {
            Location location = locations[index];
            values[index, 0] = location.Id;
            values[index, 1] = location.WarehouseId;
            values[index, 2] = location.Code;
            values[index, 3] = location.Name;
            values[index, 4] = location.CreatedAt;
            values[index, 5] = location.UpdatedAt;
        }

        return values;
    }
}
