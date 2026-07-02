using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPsychTodayPayoutTotals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Code500Deductions",
                table: "PayStatements",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PsychTodayPayout",
                table: "PayStatements",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPayout",
                table: "PayStatements",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPayout",
                table: "PayRuns",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPsychTodayPayout",
                table: "PayRuns",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code500Deductions",
                table: "PayStatements");

            migrationBuilder.DropColumn(
                name: "PsychTodayPayout",
                table: "PayStatements");

            migrationBuilder.DropColumn(
                name: "TotalPayout",
                table: "PayStatements");

            migrationBuilder.DropColumn(
                name: "TotalPayout",
                table: "PayRuns");

            migrationBuilder.DropColumn(
                name: "TotalPsychTodayPayout",
                table: "PayRuns");
        }
    }
}
