using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloneAmazonBack.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewMediaPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaPaths",
                table: "ProductReviews",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaPaths",
                table: "ProductReviews");
        }
    }
}
