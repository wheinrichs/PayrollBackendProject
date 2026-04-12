using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clinicians",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    HasPsychToday = table.Column<bool>(type: "boolean", nullable: false),
                    CostShare = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinicians", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Filename = table.Column<string>(type: "text", nullable: false),
                    UploadTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportStatus = table.Column<int>(type: "integer", nullable: false),
                    FailedItems = table.Column<int>(type: "integer", nullable: false),
                    Fingerprint = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalApplied = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAdjudicated = table.Column<decimal>(type: "numeric", nullable: false),
                    StatementTotals = table.Column<decimal>(type: "numeric", nullable: false),
                    GenerationStatus = table.Column<int>(type: "integer", nullable: false),
                    ApprovalState = table.Column<int>(type: "integer", nullable: false),
                    ApprovedRejectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedRejectedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    PracticeMateAccountName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicianId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicianCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    PayRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalPayment = table.Column<decimal>(type: "numeric", nullable: false),
                    CostShareAdjustedPayment = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAdjustment = table.Column<decimal>(type: "numeric", nullable: false),
                    ApprovalState = table.Column<int>(type: "integer", nullable: false),
                    ApprovedRejectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedRejectedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayStatements_Clinicians_ClinicianId",
                        column: x => x.ClinicianId,
                        principalTable: "Clinicians",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayStatements_PayRuns_PayRunId",
                        column: x => x.PayRunId,
                        principalTable: "PayRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EHRUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    EHRUsername = table.Column<string>(type: "text", nullable: false),
                    ClinicianID = table.Column<Guid>(type: "uuid", nullable: true),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EHRUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EHRUsers_Clinicians_ClinicianID",
                        column: x => x.ClinicianID,
                        principalTable: "Clinicians",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_EHRUsers_Users_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    Fingerprint = table.Column<string>(type: "text", nullable: false),
                    rowNumber = table.Column<int>(type: "integer", nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PayRunId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentLineItems_Clinicians_ClinicianId",
                        column: x => x.ClinicianId,
                        principalTable: "Clinicians",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentLineItems_EHRUsers_AppliedById",
                        column: x => x.AppliedById,
                        principalTable: "EHRUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentLineItems_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentLineItems_PayRuns_PayRunId",
                        column: x => x.PayRunId,
                        principalTable: "PayRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayStatementLineItem",
                columns: table => new
                {
                    PayStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentLineItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "numeric", nullable: false)
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
                name: "IX_EHRUsers_ClinicianID",
                table: "EHRUsers",
                column: "ClinicianID");

            migrationBuilder.CreateIndex(
                name: "IX_EHRUsers_UserAccountId",
                table: "EHRUsers",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_Fingerprint",
                table: "ImportBatches",
                column: "Fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLineItems_AppliedById",
                table: "PaymentLineItems",
                column: "AppliedById");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLineItems_ClinicianId",
                table: "PaymentLineItems",
                column: "ClinicianId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLineItems_Fingerprint",
                table: "PaymentLineItems",
                column: "Fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLineItems_ImportBatchId",
                table: "PaymentLineItems",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLineItems_PayRunId",
                table: "PaymentLineItems",
                column: "PayRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayStatementLineItem_PayStatementId",
                table: "PayStatementLineItem",
                column: "PayStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_PayStatements_ClinicianId",
                table: "PayStatements",
                column: "ClinicianId");

            migrationBuilder.CreateIndex(
                name: "IX_PayStatements_PayRunId",
                table: "PayStatements",
                column: "PayRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayStatementLineItem");

            migrationBuilder.DropTable(
                name: "PayStatements");

            migrationBuilder.DropTable(
                name: "PaymentLineItems");

            migrationBuilder.DropTable(
                name: "EHRUsers");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropTable(
                name: "PayRuns");

            migrationBuilder.DropTable(
                name: "Clinicians");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
