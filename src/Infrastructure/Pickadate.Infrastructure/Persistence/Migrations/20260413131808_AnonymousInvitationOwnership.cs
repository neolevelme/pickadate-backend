using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pickadate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnonymousInvitationOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "InitiatorId",
                table: "invitations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "OwnerTokenHash",
                table: "invitations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invitations_OwnerTokenHash",
                table: "invitations",
                column: "OwnerTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invitations_OwnerTokenHash",
                table: "invitations");

            migrationBuilder.DropColumn(
                name: "OwnerTokenHash",
                table: "invitations");

            migrationBuilder.AlterColumn<Guid>(
                name: "InitiatorId",
                table: "invitations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
