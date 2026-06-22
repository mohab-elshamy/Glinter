using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateExperienceRegions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Adm3Gid",
                table: "experiences",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE experiences AS e
                SET "Adm3Gid" = (
                    SELECT a.gid
                    FROM adm3 AS a
                    WHERE a.boundary_geom IS NOT NULL
                      AND ST_Covers(
                          a.boundary_geom,
                          ST_SetSRID(ST_MakePoint(e."Longitude", e."Latitude"), 4326))
                    ORDER BY a.gid
                    LIMIT 1
                );

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM experiences WHERE "Adm3Gid" IS NULL) THEN
                        RAISE EXCEPTION
                            'Cannot migrate experiences to Adm3Gid: one or more coordinates do not match an adm3 boundary.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Adm3Gid",
                table: "experiences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_experiences_AreaId",
                table: "experiences");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "experiences");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Adm3Gid",
                table: "experiences",
                column: "Adm3Gid");

            migrationBuilder.AddForeignKey(
                name: "FK_experiences_adm3_Adm3Gid",
                table: "experiences",
                column: "Adm3Gid",
                principalTable: "adm3",
                principalColumn: "gid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_experiences_adm3_Adm3Gid",
                table: "experiences");

            migrationBuilder.DropIndex(
                name: "IX_experiences_Adm3Gid",
                table: "experiences");

            migrationBuilder.DropColumn(
                name: "Adm3Gid",
                table: "experiences");

            migrationBuilder.AddColumn<Guid>(
                name: "AreaId",
                table: "experiences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_experiences_AreaId",
                table: "experiences",
                column: "AreaId");
        }
    }
}
