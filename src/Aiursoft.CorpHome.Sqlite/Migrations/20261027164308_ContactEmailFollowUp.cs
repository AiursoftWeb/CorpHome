using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.CorpHome.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class ContactEmailFollowUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AlertCenterIncidentId",
                table: "Contacts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AlertCenterMitigatedAtUtc",
                table: "Contacts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpMessageId",
                table: "Contacts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FollowUpReceivedAtUtc",
                table: "Contacts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContactMailboxCheckpoints",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UidValidity = table.Column<long>(type: "INTEGER", nullable: false),
                    LastUid = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMailboxCheckpoints", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactMailboxCheckpoints");

            migrationBuilder.DropColumn(
                name: "AlertCenterIncidentId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "AlertCenterMitigatedAtUtc",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "FollowUpMessageId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "FollowUpReceivedAtUtc",
                table: "Contacts");
        }
    }
}
