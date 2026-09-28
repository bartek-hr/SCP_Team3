using System.Text.Json;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations;

public partial class CreateWarehouses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Warehouses",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                code = table.Column<string>(type: "TEXT", nullable: false),
                name = table.Column<string>(type: "TEXT", nullable: false),
                address = table.Column<string>(type: "TEXT", nullable: false),
                city = table.Column<string>(type: "TEXT", nullable: false),
                zip_code = table.Column<string>(type: "TEXT", nullable: false),
                province = table.Column<string>(type: "TEXT", nullable: false),
                country = table.Column<string>(type: "TEXT", nullable: false),
                contact_name = table.Column<string>(type: "TEXT", nullable: false),
                contact_phone = table.Column<string>(type: "TEXT", nullable: false),
                contact_email = table.Column<string>(type: "TEXT", nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Warehouses", x => x.id);
            });

        migrationBuilder.InsertData(
            table: "Warehouses",
            columns:
            [
                "id", "code", "name", "address", "city", "zip_code", "province", "country",
                "contact_name", "contact_phone", "contact_email", "created_at", "updated_at"
            ],
            values: GetWarehouseSeedValues());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Warehouses");
    }

    private static object[,] GetWarehouseSeedValues()
    {
        using Stream stream = typeof(CreateWarehouses).Assembly.GetManifestResourceStream("CargoHUB.LegacyData.warehouseJson")
                           ?? throw new InvalidOperationException("The legacy warehouse data resource is missing.");
        List<Warehouse> warehouses = JsonSerializer.Deserialize<List<Warehouse>>(stream)
                                     ?? throw new InvalidOperationException("The legacy warehouse data is invalid.");
        object[,] values = new object[warehouses.Count, 13];

        for (int index = 0; index < warehouses.Count; index++)
        {
            Warehouse warehouse = warehouses[index];
            values[index, 0] = warehouse.Id;
            values[index, 1] = warehouse.Code;
            values[index, 2] = warehouse.Name;
            values[index, 3] = warehouse.Address;
            values[index, 4] = warehouse.City;
            values[index, 5] = warehouse.ZipCode;
            values[index, 6] = warehouse.Province;
            values[index, 7] = warehouse.Country;
            values[index, 8] = warehouse.ContactName;
            values[index, 9] = warehouse.ContactPhone;
            values[index, 10] = warehouse.ContactEmail;
            values[index, 11] = warehouse.CreatedAt;
            values[index, 12] = warehouse.UpdatedAt;
        }

        return values;
    }
}
