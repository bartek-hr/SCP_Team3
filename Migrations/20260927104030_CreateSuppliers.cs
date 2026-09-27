using System.Text.Json;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations;

public partial class CreateSuppliers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Suppliers",
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
                phone_number = table.Column<string>(type: "TEXT", nullable: false),
                reference = table.Column<string>(type: "TEXT", nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Suppliers", x => x.id);
            });

        migrationBuilder.InsertData(
            table: "Suppliers",
            columns:
            [
                "id", "code", "name", "address", "city", "zip_code", "province",
                "country", "contact_name", "phone_number", "reference", "created_at", "updated_at"
            ],
            values: GetSupplierSeedValues());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Suppliers");
    }

    private static object[,] GetSupplierSeedValues()
    {
        using Stream stream = typeof(CreateSuppliers).Assembly.GetManifestResourceStream("CargoHUB.LegacyData.supplierJson")
                           ?? throw new InvalidOperationException("The legacy supplier data resource is missing.");
        List<Supplier> suppliers = JsonSerializer.Deserialize<List<Supplier>>(stream)
                                   ?? throw new InvalidOperationException("The legacy supplier data is invalid.");
        object[,] values = new object[suppliers.Count, 13];

        for (int index = 0; index < suppliers.Count; index++)
        {
            Supplier supplier = suppliers[index];
            values[index, 0] = supplier.Id;
            values[index, 1] = supplier.Code;
            values[index, 2] = supplier.Name;
            values[index, 3] = supplier.Address;
            values[index, 4] = supplier.City;
            values[index, 5] = supplier.ZipCode;
            values[index, 6] = supplier.Province;
            values[index, 7] = supplier.Country;
            values[index, 8] = supplier.ContactName;
            values[index, 9] = supplier.PhoneNumber;
            values[index, 10] = supplier.Reference;
            values[index, 11] = supplier.CreatedAt;
            values[index, 12] = supplier.UpdatedAt;
        }

        return values;
    }
}
