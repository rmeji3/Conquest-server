using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ping.Migrations.AppDb
{
    /// <inheritdoc />
    public partial class AllowStackedReviewStickerReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReviewStickerReactions_ReviewId_UserId",
                table: "ReviewStickerReactions");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewStickerReactions_ReviewId_UserId",
                table: "ReviewStickerReactions",
                columns: new[] { "ReviewId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReviewStickerReactions_ReviewId_UserId",
                table: "ReviewStickerReactions");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewStickerReactions_ReviewId_UserId",
                table: "ReviewStickerReactions",
                columns: new[] { "ReviewId", "UserId" },
                unique: true);
        }
    }
}
