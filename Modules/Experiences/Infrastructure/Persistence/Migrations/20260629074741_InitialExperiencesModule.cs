using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialExperiencesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "experiences");

            migrationBuilder.CreateTable(
                name: "experiences",
                schema: "experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Address = table.Column<string>(type: "character varying(750)", maxLength: 750, nullable: true),
                    Cid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Adm0Gid = table.Column<int>(type: "integer", nullable: true),
                    Adm1Gid = table.Column<int>(type: "integer", nullable: true),
                    Adm2Gid = table.Column<int>(type: "integer", nullable: true),
                    Adm3Gid = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    GoogleMapsLink = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhoneInternational = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PriceRange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reviews = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Website = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "experience_amenities",
                schema: "experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_amenities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_amenities_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalSchema: "experiences",
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_featured_images",
                schema: "experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    Link = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_featured_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_featured_images_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalSchema: "experiences",
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_hours",
                schema: "experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OpensAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ClosesAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_hours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_hours_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalSchema: "experiences",
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_popular_times",
                schema: "experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HourOfDay = table.Column<int>(type: "integer", nullable: false),
                    PopularityPercentage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_popular_times", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_popular_times_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalSchema: "experiences",
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_reviews",
                schema: "experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalReviewId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewerName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    ReviewText = table.Column<string>(type: "text", nullable: true),
                    PublishedAtDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceList = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_reviews_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalSchema: "experiences",
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_reviews_per_rating",
                schema: "experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    ReviewsCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_reviews_per_rating", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_reviews_per_rating_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalSchema: "experiences",
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_experience_amenities_ExperienceId",
                schema: "experiences",
                table: "experience_amenities",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_amenities_ExperienceId_Name",
                schema: "experiences",
                table: "experience_amenities",
                columns: new[] { "ExperienceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_featured_images_ExperienceId",
                schema: "experiences",
                table: "experience_featured_images",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_featured_images_ExperienceId_Link",
                schema: "experiences",
                table: "experience_featured_images",
                columns: new[] { "ExperienceId", "Link" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_hours_ExperienceId",
                schema: "experiences",
                table: "experience_hours",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_hours_ExperienceId_DayOfWeek_OpensAt_ClosesAt",
                schema: "experiences",
                table: "experience_hours",
                columns: new[] { "ExperienceId", "DayOfWeek", "OpensAt", "ClosesAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_popular_times_ExperienceId",
                schema: "experiences",
                table: "experience_popular_times",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_popular_times_ExperienceId_DayOfWeek_HourOfDay",
                schema: "experiences",
                table: "experience_popular_times",
                columns: new[] { "ExperienceId", "DayOfWeek", "HourOfDay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_reviews_ExperienceId",
                schema: "experiences",
                table: "experience_reviews",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_reviews_ExperienceId_ExternalReviewId",
                schema: "experiences",
                table: "experience_reviews",
                columns: new[] { "ExperienceId", "ExternalReviewId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_reviews_PublishedAtDate",
                schema: "experiences",
                table: "experience_reviews",
                column: "PublishedAtDate");

            migrationBuilder.CreateIndex(
                name: "IX_experience_reviews_Rating",
                schema: "experiences",
                table: "experience_reviews",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_experience_reviews_per_rating_ExperienceId",
                schema: "experiences",
                table: "experience_reviews_per_rating",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_reviews_per_rating_ExperienceId_Rating",
                schema: "experiences",
                table: "experience_reviews_per_rating",
                columns: new[] { "ExperienceId", "Rating" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Adm0Gid",
                schema: "experiences",
                table: "experiences",
                column: "Adm0Gid");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Adm1Gid",
                schema: "experiences",
                table: "experiences",
                column: "Adm1Gid");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Adm2Gid",
                schema: "experiences",
                table: "experiences",
                column: "Adm2Gid");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Adm3Gid",
                schema: "experiences",
                table: "experiences",
                column: "Adm3Gid");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Category",
                schema: "experiences",
                table: "experiences",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Cid",
                schema: "experiences",
                table: "experiences",
                column: "Cid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Latitude_Longitude",
                schema: "experiences",
                table: "experiences",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_experiences_ProviderProfileId",
                schema: "experiences",
                table: "experiences",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Rating",
                schema: "experiences",
                table: "experiences",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_SourceType",
                schema: "experiences",
                table: "experiences",
                column: "SourceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experience_amenities",
                schema: "experiences");

            migrationBuilder.DropTable(
                name: "experience_featured_images",
                schema: "experiences");

            migrationBuilder.DropTable(
                name: "experience_hours",
                schema: "experiences");

            migrationBuilder.DropTable(
                name: "experience_popular_times",
                schema: "experiences");

            migrationBuilder.DropTable(
                name: "experience_reviews",
                schema: "experiences");

            migrationBuilder.DropTable(
                name: "experience_reviews_per_rating",
                schema: "experiences");

            migrationBuilder.DropTable(
                name: "experiences",
                schema: "experiences");
        }
    }
}
