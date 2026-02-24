using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.OrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPoFieldsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "customer_po_file_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_po_number",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_po_file_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "customer_po_number",
                table: "orders");
        }
    }
}
