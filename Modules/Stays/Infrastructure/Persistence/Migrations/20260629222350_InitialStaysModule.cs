using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialStaysModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stays");

            migrationBuilder.CreateTable(
                name: "stays",
                schema: "stays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    HotelOwnerProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    GoogleMapsLink = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Reviews = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Website = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhoneInternational = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LocationSummaryDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Cid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Adm0Gid = table.Column<int>(type: "integer", nullable: true),
                    Adm1Gid = table.Column<int>(type: "integer", nullable: true),
                    Adm2Gid = table.Column<int>(type: "integer", nullable: true),
                    Adm3Gid = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stay_amenities",
                schema: "stays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StayId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stay_amenities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stay_amenities_stays_StayId",
                        column: x => x.StayId,
                        principalSchema: "stays",
                        principalTable: "stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_booking_platforms",
                schema: "stays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StayId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    PriceWithTax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Link = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stay_booking_platforms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stay_booking_platforms_stays_StayId",
                        column: x => x.StayId,
                        principalSchema: "stays",
                        principalTable: "stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_images",
                schema: "stays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StayId = table.Column<int>(type: "integer", nullable: false),
                    Link = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stay_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stay_images_stays_StayId",
                        column: x => x.StayId,
                        principalSchema: "stays",
                        principalTable: "stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_reviews",
                schema: "stays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StayId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalReviewId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewerName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    ReviewText = table.Column<string>(type: "text", nullable: true),
                    Platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PublishedAtDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stay_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stay_reviews_stays_StayId",
                        column: x => x.StayId,
                        principalSchema: "stays",
                        principalTable: "stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stay_reviews_per_rating",
                schema: "stays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StayId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    ReviewsCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stay_reviews_per_rating", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stay_reviews_per_rating_stays_StayId",
                        column: x => x.StayId,
                        principalSchema: "stays",
                        principalTable: "stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stay_amenities_StayId",
                schema: "stays",
                table: "stay_amenities",
                column: "StayId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_amenities_StayId_Name",
                schema: "stays",
                table: "stay_amenities",
                columns: new[] { "StayId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_booking_platforms_StayId",
                schema: "stays",
                table: "stay_booking_platforms",
                column: "StayId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_booking_platforms_StayId_Name_Link",
                schema: "stays",
                table: "stay_booking_platforms",
                columns: new[] { "StayId", "Name", "Link" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_images_StayId",
                schema: "stays",
                table: "stay_images",
                column: "StayId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_images_StayId_Link",
                schema: "stays",
                table: "stay_images",
                columns: new[] { "StayId", "Link" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_Platform",
                schema: "stays",
                table: "stay_reviews",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_PublishedAtDate",
                schema: "stays",
                table: "stay_reviews",
                column: "PublishedAtDate");

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_Rating",
                schema: "stays",
                table: "stay_reviews",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_StayId",
                schema: "stays",
                table: "stay_reviews",
                column: "StayId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_StayId_ExternalReviewId",
                schema: "stays",
                table: "stay_reviews",
                columns: new[] { "StayId", "ExternalReviewId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_per_rating_StayId",
                schema: "stays",
                table: "stay_reviews_per_rating",
                column: "StayId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_per_rating_StayId_Rating",
                schema: "stays",
                table: "stay_reviews_per_rating",
                columns: new[] { "StayId", "Rating" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stays_Adm0Gid",
                schema: "stays",
                table: "stays",
                column: "Adm0Gid");

            migrationBuilder.CreateIndex(
                name: "IX_stays_Adm1Gid",
                schema: "stays",
                table: "stays",
                column: "Adm1Gid");

            migrationBuilder.CreateIndex(
                name: "IX_stays_Adm2Gid",
                schema: "stays",
                table: "stays",
                column: "Adm2Gid");

            migrationBuilder.CreateIndex(
                name: "IX_stays_Adm3Gid",
                schema: "stays",
                table: "stays",
                column: "Adm3Gid");

            migrationBuilder.CreateIndex(
                name: "IX_stays_Cid",
                schema: "stays",
                table: "stays",
                column: "Cid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stays_HotelOwnerProfileId",
                schema: "stays",
                table: "stays",
                column: "HotelOwnerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_stays_Latitude_Longitude",
                schema: "stays",
                table: "stays",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_stays_Price",
                schema: "stays",
                table: "stays",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_stays_Rating",
                schema: "stays",
                table: "stays",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_stays_SourceType",
                schema: "stays",
                table: "stays",
                column: "SourceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stay_amenities",
                schema: "stays");

            migrationBuilder.DropTable(
                name: "stay_booking_platforms",
                schema: "stays");

            migrationBuilder.DropTable(
                name: "stay_images",
                schema: "stays");

            migrationBuilder.DropTable(
                name: "stay_reviews",
                schema: "stays");

            migrationBuilder.DropTable(
                name: "stay_reviews_per_rating",
                schema: "stays");

            migrationBuilder.DropTable(
                name: "stays",
                schema: "stays");
        }
    }
}
