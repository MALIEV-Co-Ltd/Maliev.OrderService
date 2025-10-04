using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Maliev.OrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_subscriptions",
                columns: table => new
                {
                    subscription_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_subscribed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    channels = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_subscriptions", x => x.subscription_id);
                });

            migrationBuilder.CreateTable(
                name: "service_categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_categories", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "process_types",
                columns: table => new
                {
                    process_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_category_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_types", x => x.process_type_id);
                    table.ForeignKey(
                        name: "FK_process_types_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    service_category_id = table.Column<int>(type: "integer", nullable: false),
                    process_type_id = table.Column<int>(type: "integer", nullable: true),
                    material_id = table.Column<int>(type: "integer", nullable: true),
                    color_id = table.Column<int>(type: "integer", nullable: true),
                    surface_finishing_id = table.Column<int>(type: "integer", nullable: true),
                    material_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    color_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    surface_finishing_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    material_cache_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ordered_quantity = table.Column<int>(type: "integer", nullable: true),
                    manufactured_quantity = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    lead_time_days = table.Column<int>(type: "integer", nullable: true),
                    promised_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    quoted_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    quote_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValue: "THB"),
                    is_confidential = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    payment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Unpaid"),
                    assigned_employee_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    department_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    requirements = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_orders_process_types_process_type_id",
                        column: x => x.process_type_id,
                        principalTable: "process_types",
                        principalColumn: "process_type_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    audit_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    performed_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    change_details = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.audit_id);
                    table.ForeignKey(
                        name: "FK_audit_logs_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_3d_design_attributes",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    complexity_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    deliverables = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    design_software = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    revision_rounds = table.Column<int>(type: "integer", nullable: false, defaultValue: 2)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_3d_design_attributes", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_order_3d_design_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_3d_printing_attributes",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    thread_tap_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    insert_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    part_marking = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    part_assembly_test_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_3d_printing_attributes", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_order_3d_printing_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_3d_scanning_attributes",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    required_accuracy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    scan_location = table.Column<string>(type: "text", nullable: true),
                    output_file_formats = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    deviation_report_requested = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_3d_scanning_attributes", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_order_3d_scanning_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_cnc_machining_attributes",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tap_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tolerance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    surface_roughness = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    inspection_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_cnc_machining_attributes", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_order_cnc_machining_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_files",
                columns: table => new
                {
                    file_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    design_units = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    object_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    access_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Internal"),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    uploaded_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_files", x => x.file_id);
                    table.ForeignKey(
                        name: "FK_order_files_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_notes",
                columns: table => new
                {
                    note_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    note_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    note_text = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_notes", x => x.note_id);
                    table.ForeignKey(
                        name: "FK_order_notes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_sheet_metal_attributes",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    thickness = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    welding_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    welding_details = table.Column<string>(type: "text", nullable: true),
                    tolerance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    inspection_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_sheet_metal_attributes", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_order_sheet_metal_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_statuses",
                columns: table => new
                {
                    status_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
                    customer_notes = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_statuses", x => x.status_id);
                    table.ForeignKey(
                        name: "FK_order_statuses_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_OrderId",
                table: "audit_logs",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_PerformedAt",
                table: "audit_logs",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_PerformedBy",
                table: "audit_logs",
                column: "performed_by");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSubscription_CustomerId",
                table: "notification_subscriptions",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderCncMachiningAttributes_Tolerance",
                table: "order_cnc_machining_attributes",
                column: "tolerance");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFile_DeletedAt",
                table: "order_files",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFile_FileCategory",
                table: "order_files",
                column: "file_category");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFile_FileRole",
                table: "order_files",
                column: "file_role");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFile_ObjectPath",
                table: "order_files",
                column: "object_path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderFile_OrderId",
                table: "order_files",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNote_CreatedAt",
                table: "order_notes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNote_CreatedBy",
                table: "order_notes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNote_NoteType",
                table: "order_notes",
                column: "note_type");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNote_OrderId",
                table: "order_notes",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_AssignedEmployeeId",
                table: "orders",
                column: "assigned_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_CreatedAt",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_Order_CustomerId",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_DepartmentId",
                table: "orders",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_MaterialId",
                table: "orders",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_PaymentId",
                table: "orders",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_ProcessTypeId",
                table: "orders",
                column: "process_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_service_category_id",
                table: "orders",
                column: "service_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatus_OrderId",
                table: "order_statuses",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatus_Status",
                table: "order_statuses",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatus_Timestamp",
                table: "order_statuses",
                column: "timestamp");

#pragma warning disable CA1861 // Avoid constant arrays as arguments
            migrationBuilder.CreateIndex(
                name: "IX_ProcessType_ServiceCategoryId_Name",
                table: "process_types",
                columns: new[] { "service_category_id", "name" },
                unique: true);
#pragma warning restore CA1861

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategory_Name",
                table: "service_categories",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "notification_subscriptions");

            migrationBuilder.DropTable(
                name: "order_3d_design_attributes");

            migrationBuilder.DropTable(
                name: "order_3d_printing_attributes");

            migrationBuilder.DropTable(
                name: "order_3d_scanning_attributes");

            migrationBuilder.DropTable(
                name: "order_cnc_machining_attributes");

            migrationBuilder.DropTable(
                name: "order_files");

            migrationBuilder.DropTable(
                name: "order_notes");

            migrationBuilder.DropTable(
                name: "order_sheet_metal_attributes");

            migrationBuilder.DropTable(
                name: "order_statuses");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "process_types");

            migrationBuilder.DropTable(
                name: "service_categories");
        }
    }
}
