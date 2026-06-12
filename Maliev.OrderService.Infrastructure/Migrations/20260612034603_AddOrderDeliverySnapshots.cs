using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDeliverySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "billing_address_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_contact_email",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_contact_name",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_contact_phone",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "shipping_address_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_address_line1",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_address_line2",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_city",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_country",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_postal_code",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_province",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billing_address_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_contact_email",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_contact_name",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_contact_phone",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_address_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_address_line1",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_address_line2",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_city",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_country",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_postal_code",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_province",
                table: "orders");
        }
    }
}
