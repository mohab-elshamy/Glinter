using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrativeAuditRetentionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_events_CreatedAtUtc_CompletedAtUtc",
                table: "admin_audit_events",
                columns: new[] { "CreatedAtUtc", "CompletedAtUtc" },
                filter: "\"CompletedAtUtc\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admin_audit_events_CreatedAtUtc_CompletedAtUtc",
                table: "admin_audit_events");
        }
    }
}
