using System.Text.Json;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations;

public partial class CreateClients : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Clients",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
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
                table.PrimaryKey("PK_Clients", x => x.id);
            });

        migrationBuilder.InsertData(
            table: "Clients",
            columns:
            [
                "id",
                "name",
                "address",
                "city",
                "zip_code",
                "province",
                "country",
                "contact_name",
                "contact_phone",
                "contact_email",
                "created_at",
                "updated_at",
            ],
            values: GetClientSeedValues());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Clients");
    }

    private static object[,] GetClientSeedValues()
    {
        using Stream stream = typeof(CreateClients).Assembly.GetManifestResourceStream("CargoHUB.LegacyData.ClientJson")
                           ?? throw new InvalidOperationException("The legacy client data resource is missing.");
        List<Client> clients = JsonSerializer.Deserialize<List<Client>>(stream)
                      ?? throw new InvalidOperationException("The legacy client data is invalid.");
        object[,] values = new object[clients.Count, 12];

        for (int index = 0; index < clients.Count; index++)
        {
            Client client = clients[index];
            values[index, 0] = client.Id;
            values[index, 1] = client.Name;
            values[index, 2] = client.Address;
            values[index, 3] = client.City;
            values[index, 4] = client.ZipCode;
            values[index, 5] = client.Province;
            values[index, 6] = client.Country;
            values[index, 7] = client.ContactName;
            values[index, 8] = client.ContactPhone;
            values[index, 9] = client.ContactEmail;
            values[index, 10] = client.CreatedAt;
            values[index, 11] = client.UpdatedAt;
        }

        return values;
    }
}
