using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCCInventory.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickupTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PickupOrDelivery = table.Column<bool>(type: "bit", nullable: true),
                    SecondaryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecondaryPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitialContact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractSent = table.Column<bool>(type: "bit", nullable: false),
                    DayOfTextSent = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmationTextSent = table.Column<bool>(type: "bit", nullable: false),
                    TotalCost = table.Column<double>(type: "float", nullable: true),
                    DepositAmount = table.Column<double>(type: "float", nullable: true),
                    DepositPaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepositDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalPaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalPaymentDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOrderPlaced = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidInFull = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderNumber);
                });

            migrationBuilder.CreateTable(
                name: "Cake",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TierSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumTierLayers = table.Column<int>(type: "int", nullable: false),
                    CakeShape = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CakeFlavor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FillingFlavor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IcingFlavor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SplitTier = table.Column<bool>(type: "bit", nullable: false),
                    OrderNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cake", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cake_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber");
                });

            migrationBuilder.CreateTable(
                name: "Cookie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CookieType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CookieQuantity = table.Column<int>(type: "int", nullable: false),
                    OrderNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cookie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cookie_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber");
                });

            migrationBuilder.CreateTable(
                name: "Cupcake",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CupcakeSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CupcakeQuantity = table.Column<int>(type: "int", nullable: false),
                    CupcakeFlavor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FillingFlavor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IcingFlavor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupcake", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cupcake_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber");
                });

            migrationBuilder.CreateTable(
                name: "Pupcake",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PupcakeSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PupcakeQuantity = table.Column<int>(type: "int", nullable: false),
                    OrderNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pupcake", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pupcake_Orders_OrderNumber",
                        column: x => x.OrderNumber,
                        principalTable: "Orders",
                        principalColumn: "OrderNumber");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cake_OrderNumber",
                table: "Cake",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Cookie_OrderNumber",
                table: "Cookie",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Cupcake_OrderNumber",
                table: "Cupcake",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Pupcake_OrderNumber",
                table: "Pupcake",
                column: "OrderNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cake");

            migrationBuilder.DropTable(
                name: "Cookie");

            migrationBuilder.DropTable(
                name: "Cupcake");

            migrationBuilder.DropTable(
                name: "Pupcake");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
