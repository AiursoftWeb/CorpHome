using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.CorpHome.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class ContactAlertDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AlertCenterReportId",
                table: "Contacts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AlertCenterReportedAtUtc",
                table: "Contacts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertCenterReportId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "AlertCenterReportedAtUtc",
                table: "Contacts");
        }
    }
}
