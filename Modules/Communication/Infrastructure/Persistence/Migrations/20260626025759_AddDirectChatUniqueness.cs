using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectChatUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DirectKey",
                table: "chat_threads",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM (
                            SELECT
                                string_agg(p."UserId"::text, ':' ORDER BY p."UserId"::text) AS "DirectKey"
                            FROM chat_participants AS p
                            INNER JOIN chat_threads AS t ON t."Id" = p."ThreadId"
                            WHERE p."LeftAtUtc" IS NULL
                              AND t."Type" = 'Direct'
                            GROUP BY p."ThreadId"
                            HAVING COUNT(*) = 2
                        ) AS direct_keys
                        GROUP BY "DirectKey"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate direct chat threads exist. Resolve duplicates before applying AddDirectChatUniqueness.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                UPDATE chat_threads AS t
                SET "DirectKey" = direct_keys."DirectKey"
                FROM (
                    SELECT
                        "ThreadId",
                        string_agg("UserId"::text, ':' ORDER BY "UserId"::text) AS "DirectKey"
                    FROM chat_participants
                    WHERE "LeftAtUtc" IS NULL
                    GROUP BY "ThreadId"
                    HAVING COUNT(*) = 2
                ) AS direct_keys
                WHERE t."Id" = direct_keys."ThreadId"
                  AND t."Type" = 'Direct';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_chat_threads_DirectKey",
                table: "chat_threads",
                column: "DirectKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_chat_threads_DirectKey",
                table: "chat_threads");

            migrationBuilder.DropColumn(
                name: "DirectKey",
                table: "chat_threads");
        }
    }
}
