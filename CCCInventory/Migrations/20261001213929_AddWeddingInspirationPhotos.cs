using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCCInventory.Migrations
{
    /// <inheritdoc />
    public partial class AddWeddingInspirationPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CakePhotoAttachmentId",
                table: "WeddingDetails",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CupcakePhotoAttachmentId",
                table: "WeddingDetails",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CakePhotoAttachmentId",
                table: "WeddingDetails");

            migrationBuilder.DropColumn(
                name: "CupcakePhotoAttachmentId",
                table: "WeddingDetails");
        }
    }
}
