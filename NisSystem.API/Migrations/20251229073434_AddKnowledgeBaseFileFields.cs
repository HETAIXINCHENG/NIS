using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NisSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBaseFileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "KnowledgeBases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrls",
                table: "KnowledgeBases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrls",
                table: "KnowledgeBases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationFlow",
                table: "KnowledgeBases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrls",
                table: "KnowledgeBases",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "KnowledgeBases");

            migrationBuilder.DropColumn(
                name: "FileUrls",
                table: "KnowledgeBases");

            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "KnowledgeBases");

            migrationBuilder.DropColumn(
                name: "OperationFlow",
                table: "KnowledgeBases");

            migrationBuilder.DropColumn(
                name: "VideoUrls",
                table: "KnowledgeBases");
        }
    }
}
