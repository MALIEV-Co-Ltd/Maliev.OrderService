using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderBillingLegalSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.AddColumn<string>(
                name: "billing_company_name",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            _ = migrationBuilder.AddColumn<string>(
                name: "billing_vat_number",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropColumn(
                name: "billing_company_name",
                table: "orders");

            _ = migrationBuilder.DropColumn(
                name: "billing_vat_number",
                table: "orders");
        }
    }
}
