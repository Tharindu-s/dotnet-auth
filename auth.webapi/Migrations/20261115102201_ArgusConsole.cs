using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace auth.webapi.Migrations
{
    /// <inheritdoc />
    public partial class ArgusConsole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "ApplicationClient",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Existing apps keep allowing sign-ups, as they did before this column existed
            migrationBuilder.AddColumn<bool>(
                name: "RegistrationEnabled",
                table: "ApplicationClient",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationClient_IsSystem",
                table: "ApplicationClient",
                column: "IsSystem",
                unique: true,
                filter: "\"IsSystem\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationClient_IsSystem",
                table: "ApplicationClient");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "ApplicationClient");

            migrationBuilder.DropColumn(
                name: "RegistrationEnabled",
                table: "ApplicationClient");
        }
    }
}
