using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Glinter.Modules.Regions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "adm0",
                columns: table => new
                {
                    gid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    boundary_geom = table.Column<MultiPolygon>(type: "geometry(MultiPolygon, 4326)", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    flag_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adm0", x => x.gid);
                });

            migrationBuilder.CreateTable(
                name: "adm1",
                columns: table => new
                {
                    gid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    adm0_gid = table.Column<int>(type: "integer", nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    boundary_geom = table.Column<MultiPolygon>(type: "geometry(MultiPolygon, 4326)", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adm1", x => x.gid);
                    table.ForeignKey(
                        name: "FK_adm1_adm0_adm0_gid",
                        column: x => x.adm0_gid,
                        principalTable: "adm0",
                        principalColumn: "gid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "adm2",
                columns: table => new
                {
                    gid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    adm1_gid = table.Column<int>(type: "integer", nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    boundary_geom = table.Column<MultiPolygon>(type: "geometry(MultiPolygon, 4326)", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adm2", x => x.gid);
                    table.ForeignKey(
                        name: "FK_adm2_adm1_adm1_gid",
                        column: x => x.adm1_gid,
                        principalTable: "adm1",
                        principalColumn: "gid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "adm3",
                columns: table => new
                {
                    gid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    adm2_gid = table.Column<int>(type: "integer", nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    boundary_geom = table.Column<MultiPolygon>(type: "geometry(MultiPolygon, 4326)", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adm3", x => x.gid);
                    table.ForeignKey(
                        name: "FK_adm3_adm2_adm2_gid",
                        column: x => x.adm2_gid,
                        principalTable: "adm2",
                        principalColumn: "gid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_adm0_boundary_geom",
                table: "adm0",
                column: "boundary_geom")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_adm0_pcode",
                table: "adm0",
                column: "pcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adm1_adm0_gid",
                table: "adm1",
                column: "adm0_gid");

            migrationBuilder.CreateIndex(
                name: "ix_adm1_boundary_geom",
                table: "adm1",
                column: "boundary_geom")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_adm1_pcode",
                table: "adm1",
                column: "pcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adm2_adm1_gid",
                table: "adm2",
                column: "adm1_gid");

            migrationBuilder.CreateIndex(
                name: "ix_adm2_boundary_geom",
                table: "adm2",
                column: "boundary_geom")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_adm2_pcode",
                table: "adm2",
                column: "pcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adm3_adm2_gid",
                table: "adm3",
                column: "adm2_gid");

            migrationBuilder.CreateIndex(
                name: "ix_adm3_boundary_geom",
                table: "adm3",
                column: "boundary_geom")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_adm3_pcode",
                table: "adm3",
                column: "pcode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adm3");

            migrationBuilder.DropTable(
                name: "adm2");

            migrationBuilder.DropTable(
                name: "adm1");

            migrationBuilder.DropTable(
                name: "adm0");
        }
    }
}
