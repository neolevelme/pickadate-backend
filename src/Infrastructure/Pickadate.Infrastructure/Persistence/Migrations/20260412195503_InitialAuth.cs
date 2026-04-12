using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pickadate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Vibe = table.Column<int>(type: "integer", nullable: false),
                    CustomVibe = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PlaceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PlaceGoogleId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PlaceLat = table.Column<double>(type: "double precision", nullable: false),
                    PlaceLng = table.Column<double>(type: "double precision", nullable: false),
                    PlaceFormattedAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    MeetingAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    MediaUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CounterRound = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Country = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    VibePreference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "verification_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_codes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invitations_Slug",
                table: "invitations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verification_codes_Email_ExpiresAt",
                table: "verification_codes",
                columns: new[] { "Email", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invitations");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "verification_codes");
        }
    }
}
