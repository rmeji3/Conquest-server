using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ping.Migrations.AppDb
{
    /// <inheritdoc />
    public partial class EnforceUniqueReviewStickerReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the oldest row from any stacks created before reactions
            // became unique. This must run before the unique index is added.
            migrationBuilder.Sql(
                """
                DELETE FROM "ReviewStickerReactions"
                WHERE "Id" NOT IN (
                    SELECT MIN("Id")
                    FROM "ReviewStickerReactions"
                    GROUP BY "ReviewId", "UserId", "StickerId"
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewStickerReactions_ReviewId_UserId_StickerId",
                table: "ReviewStickerReactions",
                columns: new[] { "ReviewId", "UserId", "StickerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReviewStickerReactions_ReviewId_UserId_StickerId",
                table: "ReviewStickerReactions");
        }
    }
}
