using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.CorpHome.MySql.Migrations
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
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "AlertCenterReportedAtUtc",
                table: "Contacts",
                type: "datetime(6)",
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
