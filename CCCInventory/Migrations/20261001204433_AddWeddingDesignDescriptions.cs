using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCCInventory.Migrations
{
    /// <inheritdoc />
    public partial class AddWeddingDesignDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CupcakeDesignDescription",
                table: "WeddingDetails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainCakeDesignDescription",
                table: "WeddingDetails",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CupcakeDesignDescription",
                table: "WeddingDetails");

            migrationBuilder.DropColumn(
                name: "MainCakeDesignDescription",
                table: "WeddingDetails");
        }
    }
}
