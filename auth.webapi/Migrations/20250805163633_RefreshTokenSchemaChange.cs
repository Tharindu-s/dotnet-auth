using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace auth.webapi.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenSchemaChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ApplicationClientId",
                table: "RefreshTokens",
                column: "ApplicationClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_ApplicationClient_ApplicationClientId",
                table: "RefreshTokens",
                column: "ApplicationClientId",
                principalTable: "ApplicationClient",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_ApplicationClient_ApplicationClientId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ApplicationClientId",
                table: "RefreshTokens");
        }
    }
}
