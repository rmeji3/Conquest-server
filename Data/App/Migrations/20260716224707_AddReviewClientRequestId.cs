using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ping.Data.App.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewClientRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "Reviews",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_ClientRequestId",
                table: "Reviews",
                columns: new[] { "UserId", "ClientRequestId" },
                unique: true,
                filter: "\"ClientRequestId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_UserId_ClientRequestId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "Reviews");
        }
    }
}
