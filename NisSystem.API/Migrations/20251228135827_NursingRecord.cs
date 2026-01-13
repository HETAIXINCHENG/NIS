using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NisSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class NursingRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BloodPressure",
                table: "NursingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConditionAndMeasures",
                table: "NursingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntakeName",
                table: "NursingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntakeRoute",
                table: "NursingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IntakeVolume",
                table: "NursingRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputName",
                table: "NursingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutputVolume",
                table: "NursingRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pulse",
                table: "NursingRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Respiration",
                table: "NursingRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "NursingRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExistingProblems",
                table: "NursingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NursingDiagnosis",
                table: "NursingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RectificationOpinions",
                table: "NursingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "NursingPlans",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloodPressure",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "ConditionAndMeasures",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "IntakeName",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "IntakeRoute",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "IntakeVolume",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "OutputName",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "OutputVolume",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "Pulse",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "Respiration",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "NursingRecords");

            migrationBuilder.DropColumn(
                name: "ExistingProblems",
                table: "NursingPlans");

            migrationBuilder.DropColumn(
                name: "NursingDiagnosis",
                table: "NursingPlans");

            migrationBuilder.DropColumn(
                name: "RectificationOpinions",
                table: "NursingPlans");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "NursingPlans");
        }
    }
}
