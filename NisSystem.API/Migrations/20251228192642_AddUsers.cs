using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NisSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 条件删除 SortOrder 列（如果存在）
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (
                        SELECT 1 
                        FROM information_schema.columns 
                        WHERE table_name = 'Departments' 
                        AND column_name = 'SortOrder'
                    ) THEN
                        ALTER TABLE ""Departments"" DROP COLUMN ""SortOrder"";
                    END IF;
                END $$;
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Departments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
