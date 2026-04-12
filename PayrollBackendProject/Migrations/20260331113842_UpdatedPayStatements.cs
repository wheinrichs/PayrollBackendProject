using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedPayStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClinicianId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClinicianId",
                table: "Users",
                column: "ClinicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Clinicians_ClinicianId",
                table: "Users",
                column: "ClinicianId",
                principalTable: "Clinicians",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Clinicians_ClinicianId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ClinicianId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ClinicianId",
                table: "Users");
        }
    }
}
