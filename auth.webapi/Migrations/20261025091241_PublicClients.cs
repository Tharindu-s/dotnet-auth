using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace auth.webapi.Migrations
{
    /// <inheritdoc />
    public partial class PublicClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApiKeyHash",
                table: "ApplicationClient",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<List<string>>(
                name: "AllowedOrigins",
                table: "ApplicationClient",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "ApplicationClient",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedOrigins",
                table: "ApplicationClient");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "ApplicationClient");

            migrationBuilder.AlterColumn<string>(
                name: "ApiKeyHash",
                table: "ApplicationClient",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
