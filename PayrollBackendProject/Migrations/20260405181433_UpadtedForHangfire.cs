using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Migrations
{
    /// <inheritdoc />
    public partial class UpadtedForHangfire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Filepath",
                table: "ImportBatches",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Filepath",
                table: "ImportBatches");
        }
    }
}
