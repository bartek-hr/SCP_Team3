using System.Text.Json;
using System.Text.Json.Serialization;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations;

public partial class CreateTransfers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Transfers",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                reference = table.Column<string>(type: "TEXT", nullable: false),
                from_location_id = table.Column<int>(type: "INTEGER", nullable: false),
                to_location_id = table.Column<int>(type: "INTEGER", nullable: false),
                transfer_status = table.Column<string>(type: "TEXT", nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transfers", x => x.id);
                table.ForeignKey(
                    name: "FK_Transfers_Locations_from_location_id",
                    column: x => x.from_location_id,
                    principalTable: "Locations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Transfers_Locations_to_location_id",
                    column: x => x.to_location_id,
                    principalTable: "Locations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TransferItems",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                transfer_id = table.Column<int>(type: "INTEGER", nullable: false),
                item_id = table.Column<int>(type: "INTEGER", nullable: false),
                amount = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TransferItems", x => x.id);
                table.ForeignKey(
                    name: "FK_TransferItems_Transfers_transfer_id",
                    column: x => x.transfer_id,
                    principalTable: "Transfers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TransferItems_transfer_id",
            table: "TransferItems",
            column: "transfer_id");

        migrationBuilder.CreateIndex(
            name: "IX_Transfers_from_location_id",
            table: "Transfers",
            column: "from_location_id");

        migrationBuilder.CreateIndex(
            name: "IX_Transfers_to_location_id",
            table: "Transfers",
            column: "to_location_id");

        migrationBuilder.InsertData(
            table: "Transfers",
            columns: ["id", "reference", "from_location_id", "to_location_id", "transfer_status", "created_at", "updated_at"],
            values: GetTransferSeedValues());

        migrationBuilder.InsertData(
            table: "TransferItems",
            columns: ["id", "transfer_id", "item_id", "amount"],
            values: GetTransferItemSeedValues());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TransferItems");

        migrationBuilder.DropTable(name: "Transfers");
    }

    private static object[,] GetTransferSeedValues()
    {
        using Stream stream = typeof(CreateTransfers).Assembly.GetManifestResourceStream("CargoHUB.LegacyData.transferJson")
                           ?? throw new InvalidOperationException("The legacy transfer data resource is missing.");
        List<Transfer> transfers = JsonSerializer.Deserialize<List<Transfer>>(stream)
                                   ?? throw new InvalidOperationException("The legacy transfer data is invalid.");
        object[,] values = new object[transfers.Count, 7];

        for (int index = 0; index < transfers.Count; index++)
        {
            Transfer transfer = transfers[index];
            values[index, 0] = transfer.Id;
            values[index, 1] = transfer.Reference;
            values[index, 2] = transfer.FromLocationId;
            values[index, 3] = transfer.ToLocationId;
            values[index, 4] = transfer.TransferStatus.ToString();
            values[index, 5] = transfer.CreatedAt;
            values[index, 6] = transfer.UpdatedAt;
        }

        return values;
    }

    private static object[,] GetTransferItemSeedValues()
    {
        using Stream stream = typeof(CreateTransfers).Assembly.GetManifestResourceStream("CargoHUB.LegacyData.transfer_itemJson")
                           ?? throw new InvalidOperationException("The legacy transfer item data resource is missing.");
        List<LegacyTransferItem> items = JsonSerializer.Deserialize<List<LegacyTransferItem>>(stream)
                                         ?? throw new InvalidOperationException("The legacy transfer item data is invalid.");
        object[,] values = new object[items.Count, 4];

        for (int index = 0; index < items.Count; index++)
        {
            LegacyTransferItem item = items[index];
            values[index, 0] = item.Id;
            values[index, 1] = item.TransferId;
            values[index, 2] = item.ItemId;
            values[index, 3] = item.Amount;
        }

        return values;
    }

    // TransferItem hides its id and transfer_id from JSON, so the legacy rows are read with their own shape.
    private sealed record LegacyTransferItem(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("transfer_id")] int TransferId,
        [property: JsonPropertyName("item_id")] int ItemId,
        [property: JsonPropertyName("amount")] int Amount);
}
