using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Migrations
{
    /// <inheritdoc />
    public partial class EmailIsUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Clinicians_Email",
                table: "Clinicians",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clinicians_Email",
                table: "Clinicians");
        }
    }
}
