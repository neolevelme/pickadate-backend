using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pickadate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnniversaryAndRecipient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnniversaryEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RecipientId",
                table: "invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "anniversaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserBId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstDateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anniversaries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invitations_RecipientId",
                table: "invitations",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_anniversaries_UserAId_UserBId",
                table: "anniversaries",
                columns: new[] { "UserAId", "UserBId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anniversaries");

            migrationBuilder.DropIndex(
                name: "IX_invitations_RecipientId",
                table: "invitations");

            migrationBuilder.DropColumn(
                name: "AnniversaryEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RecipientId",
                table: "invitations");
        }
    }
}
