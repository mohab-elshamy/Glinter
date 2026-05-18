using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.LocationCatalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDistrictSafetySignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "district_safety_signals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RiskCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    SentimentScore = table.Column<double>(type: "double precision", nullable: false),
                    IsSafetyRelevant = table.Column<bool>(type: "boolean", nullable: false),
                    AiSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RawAiJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnalyzedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_district_safety_signals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_district_safety_signals_districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_district_safety_signals_DistrictId",
                table: "district_safety_signals",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_district_safety_signals_DistrictId_SourceType_PublishedAtUtc",
                table: "district_safety_signals",
                columns: new[] { "DistrictId", "SourceType", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_district_safety_signals_PublishedAtUtc",
                table: "district_safety_signals",
                column: "PublishedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "district_safety_signals");
        }
    }
}
