using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Glinter.Modules.SafetyIndex.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSafetyIndexModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "safety_index_results",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Adm2Gid = table.Column<int>(type: "integer", nullable: false),
                    WeeklyScore = table.Column<int>(type: "integer", nullable: true),
                    WeeklyGeneralSafetyDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WeeklyTrendingEventDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WeeklyCalculatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WeeklyNewsFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WeeklyNewsToUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WeeklyNewsItemCount = table.Column<int>(type: "integer", nullable: false),
                    HistoricalScore = table.Column<int>(type: "integer", nullable: true),
                    HistoricalGeneralSafetyDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HistoricalTrendingEventDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HistoricalCalculatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HistoricalNewsItemCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_safety_index_results", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_safety_index_results_Adm2Gid",
                table: "safety_index_results",
                column: "Adm2Gid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "safety_index_results");
        }
    }
}
