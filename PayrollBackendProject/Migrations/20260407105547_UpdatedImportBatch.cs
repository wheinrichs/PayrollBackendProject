using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedImportBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Errors",
                table: "ImportBatches",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "SuccessfulItems",
                table: "ImportBatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRows",
                table: "ImportBatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Errors",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "SuccessfulItems",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "TotalRows",
                table: "ImportBatches");
        }
    }
}
