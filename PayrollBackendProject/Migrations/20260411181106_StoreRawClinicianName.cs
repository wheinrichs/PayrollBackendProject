using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollBackendProject.Migrations
{
    /// <inheritdoc />
    public partial class StoreRawClinicianName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawClinicianName",
                table: "PaymentLineItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawClinicianName",
                table: "PaymentLineItems");
        }
    }
}
