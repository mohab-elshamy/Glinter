using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Itineraries.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreserveRichItineraryPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "itineraries",
                table: "saved_itinerary_items",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "itineraries",
                table: "saved_itinerary_items",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                schema: "itineraries",
                table: "saved_itinerary_items",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteGeometryJson",
                schema: "itineraries",
                table: "saved_itinerary_items",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteInstructionsJson",
                schema: "itineraries",
                table: "saved_itinerary_items",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteProviderFromPrevious",
                schema: "itineraries",
                table: "saved_itinerary_items",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteWarningsJson",
                schema: "itineraries",
                table: "saved_itinerary_items",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FallbackTravelMode",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginLabel",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OriginLatitude",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OriginLongitude",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pace",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlannerExplanation",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RecommendationScore",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalDistanceKm",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTravelMinutes",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TravelMode",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarningsJson",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WeatherLatitude",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeatherLocation",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WeatherLongitude",
                schema: "itineraries",
                table: "saved_itineraries",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "itineraries",
                table: "saved_itinerary_items");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "itineraries",
                table: "saved_itinerary_items");

            migrationBuilder.DropColumn(
                name: "Rating",
                schema: "itineraries",
                table: "saved_itinerary_items");

            migrationBuilder.DropColumn(
                name: "RouteGeometryJson",
                schema: "itineraries",
                table: "saved_itinerary_items");

            migrationBuilder.DropColumn(
                name: "RouteInstructionsJson",
                schema: "itineraries",
                table: "saved_itinerary_items");

            migrationBuilder.DropColumn(
                name: "RouteProviderFromPrevious",
                schema: "itineraries",
                table: "saved_itinerary_items");

            migrationBuilder.DropColumn(
                name: "RouteWarningsJson",
                schema: "itineraries",
                table: "saved_itinerary_items");

            migrationBuilder.DropColumn(
                name: "FallbackTravelMode",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "OriginLabel",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "OriginLatitude",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "OriginLongitude",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "Pace",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "PlannerExplanation",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "RecommendationScore",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "TotalDistanceKm",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "TotalTravelMinutes",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "TravelMode",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "WarningsJson",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "WeatherLatitude",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "WeatherLocation",
                schema: "itineraries",
                table: "saved_itineraries");

            migrationBuilder.DropColumn(
                name: "WeatherLongitude",
                schema: "itineraries",
                table: "saved_itineraries");
        }
    }
}
