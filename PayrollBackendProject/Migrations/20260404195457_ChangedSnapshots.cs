using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Migrations
{
    /// <inheritdoc />
    public partial class ChangedSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentLineItems_PayRuns_PayRunId",
                table: "PaymentLineItems");

            migrationBuilder.DropTable(
                name: "PayStatementLineItem");

            migrationBuilder.DropIndex(
                name: "IX_PaymentLineItems_PayRunId",
                table: "PaymentLineItems");

            migrationBuilder.DropColumn(
                name: "PayRunId",
                table: "PaymentLineItems");

            migrationBuilder.CreateTable(
                name: "PaymentSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayStatementId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentLineItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawData = table.Column<string>(type: "text", nullable: false),
                    ClinicianId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AdjustmentCode = table.Column<int>(type: "integer", nullable: false),
                    DateOfService = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    CPTCode = table.Column<string>(type: "text", nullable: false),
                    PaymentId = table.Column<string>(type: "text", nullable: false),
                    Payer = table.Column<string>(type: "text", nullable: false),
                    AppliedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSnapshots_PayRuns_PayRunId",
                        column: x => x.PayRunId,
                        principalTable: "PayRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentSnapshots_PayStatements_PayStatementId",
                        column: x => x.PayStatementId,
                        principalTable: "PayStatements",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSnapshots_PayRunId",
                table: "PaymentSnapshots",
                column: "PayRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSnapshots_PayStatementId",
                table: "PaymentSnapshots",
                column: "PayStatementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentSnapshots");

            migrationBuilder.AddColumn<Guid>(
                name: "PayRunId",
                table: "PaymentLineItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayStatementLineItem",
                columns: table => new
                {
                    PaymentLineItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayStatementLineItem", x => new { x.PaymentLineItemId, x.PayStatementId });
                    table.ForeignKey(
                        name: "FK_PayStatementLineItem_PayStatements_PayStatementId",
                        column: x => x.PayStatementId,
                        principalTable: "PayStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayStatementLineItem_PaymentLineItems_PaymentLineItemId",
                        column: x => x.PaymentLineItemId,
                        principalTable: "PaymentLineItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLineItems_PayRunId",
                table: "PaymentLineItems",
                column: "PayRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayStatementLineItem_PayStatementId",
                table: "PayStatementLineItem",
                column: "PayStatementId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentLineItems_PayRuns_PayRunId",
                table: "PaymentLineItems",
                column: "PayRunId",
                principalTable: "PayRuns",
                principalColumn: "Id");
        }
    }
}
