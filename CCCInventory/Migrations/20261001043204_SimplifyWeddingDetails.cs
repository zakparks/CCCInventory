using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCCInventory.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyWeddingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractReturnByDate",
                table: "WeddingDetails");

            migrationBuilder.DropColumn(
                name: "CupcakeDesignDescription",
                table: "WeddingDetails");

            migrationBuilder.DropColumn(
                name: "CupcakeFlavorDescription",
                table: "WeddingDetails");

            migrationBuilder.DropColumn(
                name: "KitchenCakeFlavorDescription",
                table: "WeddingDetails");

            migrationBuilder.DropColumn(
                name: "MainCakeDesignDescription",
                table: "WeddingDetails");

            migrationBuilder.DropColumn(
                name: "MainCakeFlavorDescription",
                table: "WeddingDetails");

            migrationBuilder.DropColumn(
                name: "ReceptionLocation",
                table: "WeddingDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContractReturnByDate",
                table: "WeddingDetails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CupcakeDesignDescription",
                table: "WeddingDetails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CupcakeFlavorDescription",
                table: "WeddingDetails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KitchenCakeFlavorDescription",
                table: "WeddingDetails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainCakeDesignDescription",
                table: "WeddingDetails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainCakeFlavorDescription",
                table: "WeddingDetails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceptionLocation",
                table: "WeddingDetails",
                type: "TEXT",
                nullable: true);
        }
    }
}
