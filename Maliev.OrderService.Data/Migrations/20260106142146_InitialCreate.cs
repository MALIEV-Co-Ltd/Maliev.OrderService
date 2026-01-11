using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable
#pragma warning disable CA1861 // Prefer static readonly arrays in migrations

namespace Maliev.OrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_notification_subscriptions", x => x.subscription_id);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_service_categories", x => x.category_id);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_process_types", x => x.process_type_id);
                    _ = table.ForeignKey(
                        name: "fk_process_types_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
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
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "'\\x0000000000000000'::bytea"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("pk_orders", x => x.order_id);
                    _ = table.ForeignKey(
                        name: "fk_orders_process_types_process_type_id",
                        column: x => x.process_type_id,
                        principalTable: "process_types",
                        principalColumn: "process_type_id",
                        onDelete: ReferentialAction.Restrict);
                    _ = table.ForeignKey(
                        name: "fk_orders_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: "service_categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Restrict);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_audit_logs", x => x.audit_id);
                    _ = table.ForeignKey(
                        name: "fk_audit_logs_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Restrict);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_order_3d_design_attributes", x => x.order_id);
                    _ = table.ForeignKey(
                        name: "fk_order_3d_design_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_order_3d_printing_attributes", x => x.order_id);
                    _ = table.ForeignKey(
                        name: "fk_order_3d_printing_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_order_3d_scanning_attributes", x => x.order_id);
                    _ = table.ForeignKey(
                        name: "fk_order_3d_scanning_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_order_cnc_machining_attributes", x => x.order_id);
                    _ = table.ForeignKey(
                        name: "fk_order_cnc_machining_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_order_files", x => x.file_id);
                    _ = table.ForeignKey(
                        name: "fk_order_files_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_order_notes", x => x.note_id);
                    _ = table.ForeignKey(
                        name: "fk_order_notes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_order_sheet_metal_attributes", x => x.order_id);
                    _ = table.ForeignKey(
                        name: "fk_order_sheet_metal_attributes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
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
                    _ = table.PrimaryKey("pk_order_statuses", x => x.status_id);
                    _ = table.ForeignKey(
                        name: "fk_order_statuses_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateIndex(
                name: "ix__audit_log__action",
                table: "audit_logs",
                column: "action");

            _ = migrationBuilder.CreateIndex(
                name: "ix__audit_log__order_id",
                table: "audit_logs",
                column: "order_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__audit_log__performed_at",
                table: "audit_logs",
                column: "performed_at");

            _ = migrationBuilder.CreateIndex(
                name: "ix__audit_log__performed_by",
                table: "audit_logs",
                column: "performed_by");

            _ = migrationBuilder.CreateIndex(
                name: "ix__notification_subscription__customer_id",
                table: "notification_subscriptions",
                column: "customer_id",
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_cnc_machining_attributes__tolerance",
                table: "order_cnc_machining_attributes",
                column: "tolerance");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_file__deleted_at",
                table: "order_files",
                column: "deleted_at");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_file__file_category",
                table: "order_files",
                column: "file_category");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_file__file_role",
                table: "order_files",
                column: "file_role");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_file__object_path",
                table: "order_files",
                column: "object_path",
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_file__order_id",
                table: "order_files",
                column: "order_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_note__created_at",
                table: "order_notes",
                column: "created_at");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_note__created_by",
                table: "order_notes",
                column: "created_by");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_note__note_type",
                table: "order_notes",
                column: "note_type");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_note__order_id",
                table: "order_notes",
                column: "order_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order__assigned_employee_id",
                table: "orders",
                column: "assigned_employee_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order__created_at",
                table: "orders",
                column: "created_at");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order__customer_id",
                table: "orders",
                column: "customer_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order__department_id",
                table: "orders",
                column: "department_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order__material_id",
                table: "orders",
                column: "material_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order__payment_id",
                table: "orders",
                column: "payment_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order__process_type_id",
                table: "orders",
                column: "process_type_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix_orders_service_category_id",
                table: "orders",
                column: "service_category_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_status__order_id",
                table: "order_statuses",
                column: "order_id");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_status__status",
                table: "order_statuses",
                column: "status");

            _ = migrationBuilder.CreateIndex(
                name: "ix__order_status__timestamp",
                table: "order_statuses",
                column: "timestamp");

            _ = migrationBuilder.CreateIndex(
                name: "ix__process_type__service_category_id__name",
                table: "process_types",
                columns: ["service_category_id", "name"],
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "ix__service_category__name",
                table: "service_categories",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "audit_logs");

            _ = migrationBuilder.DropTable(
                name: "notification_subscriptions");

            _ = migrationBuilder.DropTable(
                name: "order_3d_design_attributes");

            _ = migrationBuilder.DropTable(
                name: "order_3d_printing_attributes");

            _ = migrationBuilder.DropTable(
                name: "order_3d_scanning_attributes");

            _ = migrationBuilder.DropTable(
                name: "order_cnc_machining_attributes");

            _ = migrationBuilder.DropTable(
                name: "order_files");

            _ = migrationBuilder.DropTable(
                name: "order_notes");

            _ = migrationBuilder.DropTable(
                name: "order_sheet_metal_attributes");

            _ = migrationBuilder.DropTable(
                name: "order_statuses");

            _ = migrationBuilder.DropTable(
                name: "orders");

            _ = migrationBuilder.DropTable(
                name: "process_types");

            _ = migrationBuilder.DropTable(
                name: "service_categories");
        }
    }
}
