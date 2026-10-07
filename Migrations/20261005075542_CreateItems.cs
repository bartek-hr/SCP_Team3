using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations
{
    /// <inheritdoc />
    public partial class CreateItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemGroups",
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
                    table.PrimaryKey("PK_ItemGroups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ItemLines",
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
                    table.PrimaryKey("PK_ItemLines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    code = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    barcode = table.Column<string>(type: "TEXT", nullable: false),
                    model_number = table.Column<string>(type: "TEXT", nullable: false),
                    commodity_code = table.Column<int>(type: "INTEGER", nullable: false),
                    unit_weight = table.Column<decimal>(type: "TEXT", nullable: false),
                    item_line_id = table.Column<int>(type: "INTEGER", nullable: false),
                    item_group_id = table.Column<int>(type: "INTEGER", nullable: false),
                    item_type_id = table.Column<int>(type: "INTEGER", nullable: false),
                    min_purchase_qty = table.Column<int>(type: "INTEGER", nullable: false),
                    case_size = table.Column<int>(type: "INTEGER", nullable: false),
                    packaging_type = table.Column<string>(type: "TEXT", nullable: false),
                    order_multiple = table.Column<int>(type: "INTEGER", nullable: false),
                    supplier_id = table.Column<int>(type: "INTEGER", nullable: false),
                    supplier_sku = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.id);
                    table.ForeignKey(
                        name: "FK_Items_ItemGroups_item_group_id",
                        column: x => x.item_group_id,
                        principalTable: "ItemGroups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_ItemLines_item_line_id",
                        column: x => x.item_line_id,
                        principalTable: "ItemLines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_item_group_id",
                table: "Items",
                column: "item_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_Items_item_line_id",
                table: "Items",
                column: "item_line_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "ItemGroups");

            migrationBuilder.DropTable(
                name: "ItemLines");
        }
    }
}
