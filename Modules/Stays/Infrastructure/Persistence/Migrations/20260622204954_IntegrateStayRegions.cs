using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateStayRegions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Adm3Gid",
                table: "stays",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE stays AS s
                SET "Adm3Gid" = (
                    SELECT a.gid
                    FROM adm3 AS a
                    WHERE a.boundary_geom IS NOT NULL
                      AND ST_Covers(
                          a.boundary_geom,
                          ST_SetSRID(ST_MakePoint(s."Longitude", s."Latitude"), 4326))
                    ORDER BY a.gid
                    LIMIT 1
                );

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM stays WHERE "Adm3Gid" IS NULL) THEN
                        RAISE EXCEPTION
                            'Cannot migrate stays to Adm3Gid: one or more coordinates do not match an adm3 boundary.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Adm3Gid",
                table: "stays",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "stays");

            migrationBuilder.CreateIndex(
                name: "IX_stays_Adm3Gid",
                table: "stays",
                column: "Adm3Gid");

            migrationBuilder.AddForeignKey(
                name: "FK_stays_adm3_Adm3Gid",
                table: "stays",
                column: "Adm3Gid",
                principalTable: "adm3",
                principalColumn: "gid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stays_adm3_Adm3Gid",
                table: "stays");

            migrationBuilder.DropIndex(
                name: "IX_stays_Adm3Gid",
                table: "stays");

            migrationBuilder.DropColumn(
                name: "Adm3Gid",
                table: "stays");

            migrationBuilder.AddColumn<Guid>(
                name: "AreaId",
                table: "stays",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
