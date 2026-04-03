using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutsourcingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_outsourced",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "supplier_cost_thb",
                table: "orders",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "supplier_estimated_delivery",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_name",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_outsourced",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "supplier_cost_thb",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "supplier_estimated_delivery",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "orders");
        }
    }
}
