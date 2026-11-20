using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuracDev.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameLinkProfileToAudience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Profile",
                table: "application_links",
                newName: "Audience");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Audience",
                table: "application_links",
                newName: "Profile");
        }
    }
}
