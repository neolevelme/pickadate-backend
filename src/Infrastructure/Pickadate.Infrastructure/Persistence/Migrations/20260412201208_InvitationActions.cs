using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pickadate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InvitationActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeclineReason",
                table: "invitations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "counter_proposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    ProposerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    NewMeetingAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    new_place_google_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    new_place_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    new_place_formatted_address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    new_place_lat = table.Column<double>(type: "double precision", nullable: true),
                    new_place_lng = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counter_proposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "decline_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decline_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_counter_proposals_InvitationId_Round",
                table: "counter_proposals",
                columns: new[] { "InvitationId", "Round" });

            migrationBuilder.CreateIndex(
                name: "IX_decline_records_Ip_CreatedAt",
                table: "decline_records",
                columns: new[] { "Ip", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "counter_proposals");

            migrationBuilder.DropTable(
                name: "decline_records");

            migrationBuilder.DropColumn(
                name: "DeclineReason",
                table: "invitations");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "invitations");
        }
    }
}
