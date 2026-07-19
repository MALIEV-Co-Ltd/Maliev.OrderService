using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderQuoteVersionReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "quote_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "quote_number",
                table: "orders",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "quote_version_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quote_version_number",
                table: "orders",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quote_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "quote_number",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "quote_version_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "quote_version_number",
                table: "orders");
        }
    }
}
