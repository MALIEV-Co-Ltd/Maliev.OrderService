using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.OrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConcurrencyAndNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropColumn(
                name: "version",
                table: "orders");

            _ = migrationBuilder.CreateSequence(
                name: "order_id_seq");

            /*
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "orders");
            */

            _ = migrationBuilder.DropSequence(
                name: "order_id_seq");

            _ = migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "orders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "'\\x0000000000000000'::bytea");
        }
    }
}
