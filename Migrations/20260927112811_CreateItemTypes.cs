using System;
using System.Text.Json;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations
{
    /// <inheritdoc />
    public partial class CreateItemTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTypes", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "ItemTypes",
                columns: ["id", "name", "description", "created_at", "updated_at"],
                values: GetItemTypeSeedValues());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ItemTypes");
        }

        private static object[,] GetItemTypeSeedValues()
        {
            using Stream stream = typeof(CreateItemTypes).Assembly.GetManifestResourceStream("CargoHUB.LegacyData.item_typeJson")
                                 ?? throw new InvalidOperationException("The legacy item type data resource is missing.");
            List<ItemType> itemTypes = JsonSerializer.Deserialize<List<ItemType>>(stream)
                                       ?? throw new InvalidOperationException("The legacy item type data is invalid.");
            object[,] values = new object[itemTypes.Count, 5];

            for (int index = 0; index < itemTypes.Count; index++)
            {
                ItemType itemType = itemTypes[index];
                values[index, 0] = itemType.Id;
                values[index, 1] = itemType.Name;
                values[index, 2] = itemType.Description;
                values[index, 3] = itemType.CreatedAt;
                values[index, 4] = itemType.UpdatedAt;
            }

            return values;
        }
    }
}
