using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCode500Applications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Code500AppliedAmount",
                table: "PaymentLineItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Code500Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentLineItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayRunId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Code500Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Code500Applications_PayRuns_PayRunId",
                        column: x => x.PayRunId,
                        principalTable: "PayRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Code500Applications_PaymentLineItems_PaymentLineItemId",
                        column: x => x.PaymentLineItemId,
                        principalTable: "PaymentLineItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Code500Applications_PaymentLineItemId",
                table: "Code500Applications",
                column: "PaymentLineItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Code500Applications_PayRunId",
                table: "Code500Applications",
                column: "PayRunId");

            // Backfill: rows already marked fully applied under the old all-or-nothing model need
            // their cumulative applied amount seeded, so they don't reappear as outstanding balance.
            migrationBuilder.Sql(
                "UPDATE \"PaymentLineItems\" SET \"Code500AppliedAmount\" = ABS(\"AdjustmentAmount\") WHERE \"IsCode500Applied\" = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Code500Applications");

            migrationBuilder.DropColumn(
                name: "Code500AppliedAmount",
                table: "PaymentLineItems");
        }
    }
}
