using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenExperienceIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_experiences_ProviderProfileId_Adm3Gid_Title",
                table: "experiences",
                columns: new[] { "ProviderProfileId", "Adm3Gid", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_AvailabilityId_TravelerProfileId",
                table: "experience_bookings",
                columns: new[] { "AvailabilityId", "TravelerProfileId" },
                unique: true,
                filter: "\"Status\" <> 'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_experiences_ProviderProfileId_Adm3Gid_Title",
                table: "experiences");

            migrationBuilder.DropIndex(
                name: "IX_experience_bookings_AvailabilityId_TravelerProfileId",
                table: "experience_bookings");
        }
    }
}
