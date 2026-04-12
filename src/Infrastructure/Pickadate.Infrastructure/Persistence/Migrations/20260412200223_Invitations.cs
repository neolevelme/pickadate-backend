using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pickadate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Invitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlaceName",
                table: "invitations",
                newName: "place_name");

            migrationBuilder.RenameColumn(
                name: "PlaceLng",
                table: "invitations",
                newName: "place_lng");

            migrationBuilder.RenameColumn(
                name: "PlaceLat",
                table: "invitations",
                newName: "place_lat");

            migrationBuilder.RenameColumn(
                name: "PlaceGoogleId",
                table: "invitations",
                newName: "place_google_id");

            migrationBuilder.RenameColumn(
                name: "PlaceFormattedAddress",
                table: "invitations",
                newName: "place_formatted_address");

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstViewedAt",
                table: "invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invitations_InitiatorId",
                table: "invitations",
                column: "InitiatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invitations_InitiatorId",
                table: "invitations");

            migrationBuilder.DropColumn(
                name: "FirstViewedAt",
                table: "invitations");

            migrationBuilder.RenameColumn(
                name: "place_name",
                table: "invitations",
                newName: "PlaceName");

            migrationBuilder.RenameColumn(
                name: "place_lng",
                table: "invitations",
                newName: "PlaceLng");

            migrationBuilder.RenameColumn(
                name: "place_lat",
                table: "invitations",
                newName: "PlaceLat");

            migrationBuilder.RenameColumn(
                name: "place_google_id",
                table: "invitations",
                newName: "PlaceGoogleId");

            migrationBuilder.RenameColumn(
                name: "place_formatted_address",
                table: "invitations",
                newName: "PlaceFormattedAddress");
        }
    }
}
