using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NisSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "MedicationOrders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specification",
                table: "MedicationOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "MedicationOrders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "MedicationOrders");

            migrationBuilder.DropColumn(
                name: "Specification",
                table: "MedicationOrders");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "MedicationOrders");
        }
    }
}
