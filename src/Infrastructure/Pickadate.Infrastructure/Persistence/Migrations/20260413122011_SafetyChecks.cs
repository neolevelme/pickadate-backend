using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pickadate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SafetyChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "safety_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FriendToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScheduledCheckInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AlertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_safety_checks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_safety_checks_FriendToken",
                table: "safety_checks",
                column: "FriendToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_safety_checks_InvitationId_UserId",
                table: "safety_checks",
                columns: new[] { "InvitationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_safety_checks_ScheduledCheckInAt",
                table: "safety_checks",
                column: "ScheduledCheckInAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "safety_checks");
        }
    }
}
