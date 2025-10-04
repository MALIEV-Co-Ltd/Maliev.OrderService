using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.OrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVersionColumnToBeGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add default value for version column (empty 8-byte array for PostgreSQL bytea)
            migrationBuilder.Sql(@"
                ALTER TABLE orders
                ALTER COLUMN version SET DEFAULT '\\x0000000000000000'::bytea;
            ");

            // Update existing rows that might have NULL version
            migrationBuilder.Sql(@"
                UPDATE orders
                SET version = '\\x0000000000000000'::bytea
                WHERE version IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
