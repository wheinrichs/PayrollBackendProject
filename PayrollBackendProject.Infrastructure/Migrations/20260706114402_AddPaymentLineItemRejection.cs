using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentLineItemRejection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRejected",
                table: "PaymentLineItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedById",
                table: "PaymentLineItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedDate",
                table: "PaymentLineItems",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRejected",
                table: "PaymentLineItems");

            migrationBuilder.DropColumn(
                name: "RejectedById",
                table: "PaymentLineItems");

            migrationBuilder.DropColumn(
                name: "RejectedDate",
                table: "PaymentLineItems");
        }
    }
}
