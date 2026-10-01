using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCCInventory.Migrations
{
    /// <inheritdoc />
    public partial class AddWeddingOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWedding",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GoogleTokens",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleTokens", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "WeddingDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    EventDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReceptionLocation = table.Column<string>(type: "TEXT", nullable: true),
                    CeremonySameLocation = table.Column<bool>(type: "INTEGER", nullable: true),
                    CeremonyTime = table.Column<string>(type: "TEXT", nullable: true),
                    ReceptionTime = table.Column<string>(type: "TEXT", nullable: true),
                    Partner2Name = table.Column<string>(type: "TEXT", nullable: true),
                    Partner2Phone = table.Column<string>(type: "TEXT", nullable: true),
                    DayOfContactTitle = table.Column<string>(type: "TEXT", nullable: true),
                    VenueContactName = table.Column<string>(type: "TEXT", nullable: true),
                    VenueContactPhone = table.Column<string>(type: "TEXT", nullable: true),
                    ContractReturnByDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CakeBoardColor = table.Column<string>(type: "TEXT", nullable: true),
                    CakeTopper = table.Column<bool>(type: "INTEGER", nullable: true),
                    HasFlowers = table.Column<bool>(type: "INTEGER", nullable: true),
                    FlowerType = table.Column<string>(type: "TEXT", nullable: true),
                    FlowersProvidedBy = table.Column<string>(type: "TEXT", nullable: true),
                    FloristName = table.Column<string>(type: "TEXT", nullable: true),
                    FloristPhone = table.Column<string>(type: "TEXT", nullable: true),
                    FloristDeliveryTime = table.Column<string>(type: "TEXT", nullable: true),
                    DeliveryWindowEnd = table.Column<string>(type: "TEXT", nullable: true),
                    PickupPersonName = table.Column<string>(type: "TEXT", nullable: true),
                    PickupPersonPhone = table.Column<string>(type: "TEXT", nullable: true),
                    MainCakeFlavorDescription = table.Column<string>(type: "TEXT", nullable: true),
                    MainCakeDesignDescription = table.Column<string>(type: "TEXT", nullable: true),
                    KitchenCakeFlavorDescription = table.Column<string>(type: "TEXT", nullable: true),
                    CupcakeFlavorDescription = table.Column<string>(type: "TEXT", nullable: true),
                    CupcakeDesignDescription = table.Column<string>(type: "TEXT", nullable: true),
                    TotalServings = table.Column<int>(type: "INTEGER", nullable: true),
                    ContractDocId = table.Column<string>(type: "TEXT", nullable: true),
                    ContractDocUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ContractDocName = table.Column<string>(type: "TEXT", nullable: true),
                    ContractGeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeddingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeddingDetails_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeddingDetails_OrderNumber",
                table: "WeddingDetails",
                column: "OrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleTokens");

            migrationBuilder.DropTable(
                name: "WeddingDetails");

            migrationBuilder.DropColumn(
                name: "IsWedding",
                table: "Orders");
        }
    }
}
