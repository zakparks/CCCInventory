using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCCInventory.Migrations
{
    /// <inheritdoc />
    public partial class Phase5OrderFormOverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename DeleteFlag → CancelledFlag, preserving any existing soft-delete data
            migrationBuilder.RenameColumn(
                name: "DeleteFlag",
                table: "Orders",
                newName: "CancelledFlag");

            // Add OrderType before dropping PickupOrDelivery so we can copy the data
            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            // Copy existing bool data: false → "Pickup", true → "Delivery"
            migrationBuilder.Sql("UPDATE Orders SET OrderType = CASE WHEN PickupOrDelivery = 1 THEN 'Delivery' WHEN PickupOrDelivery = 0 THEN 'Pickup' ELSE NULL END");

            migrationBuilder.DropColumn(
                name: "PickupOrDelivery",
                table: "Orders");

            migrationBuilder.AddColumn<bool>(
                name: "IsReadyForPickup",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FlavorUpgrade",
                table: "Orders",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Labor",
                table: "Orders",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LookbookPrice",
                table: "Orders",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CookieSize",
                table: "Cookies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Flavor2",
                table: "Cakes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LayerFlavors",
                table: "Cakes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OtherItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Item = table.Column<string>(type: "TEXT", nullable: true),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherItems_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtherItems_OrderNumber",
                table: "OtherItems",
                column: "OrderNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtherItems");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledFlag",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FlavorUpgrade",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Labor",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LookbookPrice",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CookieSize",
                table: "Cookies");

            migrationBuilder.DropColumn(
                name: "Flavor2",
                table: "Cakes");

            migrationBuilder.DropColumn(
                name: "LayerFlavors",
                table: "Cakes");

            migrationBuilder.DropColumn(
                name: "IsReadyForPickup",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "CancelledFlag",
                table: "Orders",
                newName: "DeleteFlag");

            migrationBuilder.AddColumn<bool>(
                name: "PickupOrDelivery",
                table: "Orders",
                type: "INTEGER",
                nullable: true);
        }
    }
}
