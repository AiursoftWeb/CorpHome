using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.CorpHome.MySql.Migrations
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
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "AlertCenterMitigatedAtUtc",
                table: "Contacts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpMessageId",
                table: "Contacts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "FollowUpReceivedAtUtc",
                table: "Contacts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContactMailboxCheckpoints",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UidValidity = table.Column<long>(type: "bigint", nullable: false),
                    LastUid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMailboxCheckpoints", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
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
