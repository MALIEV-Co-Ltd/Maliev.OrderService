using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.OrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedServiceData : Migration
    {
        /// <inheritdoc />
#pragma warning disable CA1861 // Avoid constant arrays as arguments
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed Service Categories
            migrationBuilder.InsertData(
                table: "service_categories",
                columns: new[] { "category_id", "name", "description", "is_active" },
                values: new object[,]
                {
                    { 1, "3D Printing", "Additive manufacturing services", true },
                    { 2, "3D Scanning", "3D digitization services", true },
                    { 3, "3D Design Services", "CAD modeling and design services", true },
                    { 4, "Reverse Engineering", "Reverse engineering services", true },
                    { 5, "CNC Machining", "Subtractive manufacturing services", true },
                    { 6, "Silicone Mold Making", "Silicone mold fabrication", true },
                    { 7, "Casting Services", "Metal and resin casting", true },
                    { 8, "Injection Molding Services", "Plastic injection molding", true },
                    { 9, "Surface Finishing Services", "Post-processing and finishing", true },
                    { 10, "Wire Cut", "Wire EDM cutting services", true },
                    { 11, "EDM", "Electrical Discharge Machining", true }
                });

            // Seed Process Types - 3D Printing
            migrationBuilder.InsertData(
                table: "process_types",
                columns: new[] { "service_category_id", "name", "description", "is_active" },
                values: new object[,]
                {
                    { 1, "FDM", "Fused Deposition Modeling", true },
                    { 1, "DLP", "Digital Light Processing", true },
                    { 1, "SLA", "Stereolithography", true },
                    { 1, "DMLS", "Direct Metal Laser Sintering", true },
                    { 1, "SLS", "Selective Laser Sintering", true },
                    { 1, "MJF", "Multi Jet Fusion", true },

                    // 3D Scanning
                    { 2, "Structured Light", "Structured light scanning", true },
                    { 2, "Laser Scanning", "Laser-based 3D scanning", true },
                    { 2, "On-site Scanning", "On-site 3D scanning services", true },
                    { 2, "In-house Scanning", "In-house 3D scanning", true },

                    // CNC Machining
                    { 5, "3-Axis Milling", "3-axis CNC milling", true },
                    { 5, "4-Axis Milling", "4-axis CNC milling", true },
                    { 5, "5-Axis Milling", "5-axis CNC milling", true },
                    { 5, "CNC Turning (Lathe)", "CNC lathe operations", true }
                });
        }
#pragma warning restore CA1861

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seed data in reverse order
            migrationBuilder.DeleteData(table: "process_types", keyColumn: "service_category_id", keyValue: 1);
            migrationBuilder.DeleteData(table: "process_types", keyColumn: "service_category_id", keyValue: 2);
            migrationBuilder.DeleteData(table: "process_types", keyColumn: "service_category_id", keyValue: 5);

            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 1);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 2);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 3);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 4);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 5);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 6);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 7);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 8);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 9);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 10);
            migrationBuilder.DeleteData(table: "service_categories", keyColumn: "category_id", keyValue: 11);
        }
    }
}
