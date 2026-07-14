using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Ping.Data.Auth;

#nullable disable

namespace Ping.Migrations
{
    /// <summary>
    /// Adds AspNetUsers.IsFoundingMember and backfills the first 20 verified users.
    /// This migration originally shipped without the [DbContext]/[Migration] attributes,
    /// so EF never discovered it: prod got the column applied manually, while fresh
    /// databases were created without it. The whole body is guarded on the column not
    /// existing so environments that already have it (prod) just record the history row
    /// without re-running the one-time IsVerified/IsFoundingMember backfill.
    /// </summary>
    [DbContext(typeof(AuthDbContext))]
    [Migration("20260608210000_AddIsFoundingMemberToAppUser")]
    public partial class AddIsFoundingMemberToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'AspNetUsers' AND column_name = 'IsFoundingMember'
                    ) THEN
                        ALTER TABLE "AspNetUsers"
                        ADD COLUMN "IsFoundingMember" boolean NOT NULL DEFAULT FALSE;

                        UPDATE "AspNetUsers"
                        SET "IsVerified" = TRUE;

                        UPDATE "AspNetUsers"
                        SET "IsFoundingMember" = TRUE
                        WHERE "Id" IN (
                            SELECT "Id"
                            FROM "AspNetUsers"
                            WHERE "EmailConfirmed" = TRUE
                            ORDER BY "CreatedUtc" ASC
                            LIMIT 20
                        );
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFoundingMember",
                table: "AspNetUsers");
        }
    }
}
