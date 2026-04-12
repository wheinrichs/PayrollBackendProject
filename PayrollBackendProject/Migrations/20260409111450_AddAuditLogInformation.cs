using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentLineItems_Clinicians_ClinicianId",
                table: "PaymentLineItems");

            migrationBuilder.RenameColumn(
                name: "rowNumber",
                table: "PaymentLineItems",
                newName: "RowNumber");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClinicianId",
                table: "PaymentSnapshots",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClinicianId",
                table: "PaymentLineItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "PaymentLineItemStatus",
                table: "PaymentLineItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnresolvedRows",
                table: "ImportBatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimestampUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    OriginalData = table.Column<string>(type: "text", nullable: false),
                    UpdatedData = table.Column<string>(type: "text", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentLineItems_Clinicians_ClinicianId",
                table: "PaymentLineItems",
                column: "ClinicianId",
                principalTable: "Clinicians",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentLineItems_Clinicians_ClinicianId",
                table: "PaymentLineItems");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PaymentLineItemStatus",
                table: "PaymentLineItems");

            migrationBuilder.DropColumn(
                name: "UnresolvedRows",
                table: "ImportBatches");

            migrationBuilder.RenameColumn(
                name: "RowNumber",
                table: "PaymentLineItems",
                newName: "rowNumber");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClinicianId",
                table: "PaymentSnapshots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClinicianId",
                table: "PaymentLineItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentLineItems_Clinicians_ClinicianId",
                table: "PaymentLineItems",
                column: "ClinicianId",
                principalTable: "Clinicians",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
