using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderProductionItemsSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "production_items_json",
                table: "orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "production_items_json",
                table: "orders");
        }
    }
}
