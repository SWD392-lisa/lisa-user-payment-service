using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLucy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorUserModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_at",
                table: "user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                table: "user",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "user");

            migrationBuilder.DropColumn(
                name: "suspension_reason",
                table: "user");
        }
    }
}
