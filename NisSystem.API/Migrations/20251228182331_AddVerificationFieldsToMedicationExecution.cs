using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NisSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationFieldsToMedicationExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedByUserId",
                table: "MedicationExecutions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedTime",
                table: "MedicationExecutions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationExecutions_VerifiedByUserId",
                table: "MedicationExecutions",
                column: "VerifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationExecutions_Users_VerifiedByUserId",
                table: "MedicationExecutions",
                column: "VerifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationExecutions_Users_VerifiedByUserId",
                table: "MedicationExecutions");

            migrationBuilder.DropIndex(
                name: "IX_MedicationExecutions_VerifiedByUserId",
                table: "MedicationExecutions");

            migrationBuilder.DropColumn(
                name: "VerifiedByUserId",
                table: "MedicationExecutions");

            migrationBuilder.DropColumn(
                name: "VerifiedTime",
                table: "MedicationExecutions");
        }
    }
}
