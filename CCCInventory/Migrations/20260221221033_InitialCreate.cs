using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCCInventory.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveryLocation = table.Column<string>(type: "TEXT", nullable: true),
                    CustName = table.Column<string>(type: "TEXT", nullable: true),
                    CustEmail = table.Column<string>(type: "TEXT", nullable: true),
                    CustPhone = table.Column<string>(type: "TEXT", nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    PickupOrDelivery = table.Column<bool>(type: "INTEGER", nullable: true),
                    SecondaryName = table.Column<string>(type: "TEXT", nullable: true),
                    SecondaryPhone = table.Column<string>(type: "TEXT", nullable: true),
                    InitialContact = table.Column<string>(type: "TEXT", nullable: true),
                    ContractSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    DayOfTextSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfirmationTextSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalCost = table.Column<double>(type: "REAL", nullable: true),
                    DepositAmount = table.Column<double>(type: "REAL", nullable: true),
                    DepositPaymentMethod = table.Column<string>(type: "TEXT", nullable: true),
                    DepositDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalPaymentMethod = table.Column<string>(type: "TEXT", nullable: true),
                    FinalPaymentDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateOrderPlaced = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaidInFull = table.Column<bool>(type: "INTEGER", nullable: true),
                    DeleteFlag = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderNumber);
                });

            migrationBuilder.CreateTable(
                name: "Cakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TierSize = table.Column<string>(type: "TEXT", nullable: false),
                    NumTierLayers = table.Column<int>(type: "INTEGER", nullable: false),
                    CakeShape = table.Column<string>(type: "TEXT", nullable: false),
                    CakeFlavor = table.Column<string>(type: "TEXT", nullable: false),
                    FillingFlavor = table.Column<string>(type: "TEXT", nullable: false),
                    IcingFlavor = table.Column<string>(type: "TEXT", nullable: false),
                    SplitTier = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cakes_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber");
                });

            migrationBuilder.CreateTable(
                name: "Cookies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CookieType = table.Column<string>(type: "TEXT", nullable: false),
                    CookieQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cookies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cookies_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber");
                });

            migrationBuilder.CreateTable(
                name: "Cupcakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CupcakeSize = table.Column<string>(type: "TEXT", nullable: false),
                    CupcakeQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CupcakeFlavor = table.Column<string>(type: "TEXT", nullable: false),
                    FillingFlavor = table.Column<string>(type: "TEXT", nullable: false),
                    IcingFlavor = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupcakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cupcakes_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber");
                });

            migrationBuilder.CreateTable(
                name: "Pupcakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PupcakeSize = table.Column<string>(type: "TEXT", nullable: false),
                    PupcakeQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pupcakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pupcakes_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cakes_OrderNumber",
                table: "Cakes",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Cookies_OrderNumber",
                table: "Cookies",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Cupcakes_OrderNumber",
                table: "Cupcakes",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Pupcakes_OrderNumber",
                table: "Pupcakes",
                column: "OrderNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cakes");

            migrationBuilder.DropTable(
                name: "Cookies");

            migrationBuilder.DropTable(
                name: "Cupcakes");

            migrationBuilder.DropTable(
                name: "OrderAttachments");

            migrationBuilder.DropTable(
                name: "Pupcakes");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
