using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Code500ApplyAndManualEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentLineItems_ImportBatches_ImportBatchId",
                table: "PaymentLineItems");

            migrationBuilder.AddColumn<decimal>(
                name: "GrossPaymentTotal",
                table: "PayRuns",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCode500Deductions",
                table: "PayRuns",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "PaymentSnapshots",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "PaymentLineItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "IsCode500Applied",
                table: "PaymentLineItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentLineItems_ImportBatches_ImportBatchId",
                table: "PaymentLineItems",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentLineItems_ImportBatches_ImportBatchId",
                table: "PaymentLineItems");

            migrationBuilder.DropColumn(
                name: "GrossPaymentTotal",
                table: "PayRuns");

            migrationBuilder.DropColumn(
                name: "TotalCode500Deductions",
                table: "PayRuns");

            migrationBuilder.DropColumn(
                name: "IsCode500Applied",
                table: "PaymentLineItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "PaymentSnapshots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ImportBatchId",
                table: "PaymentLineItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentLineItems_ImportBatches_ImportBatchId",
                table: "PaymentLineItems",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
