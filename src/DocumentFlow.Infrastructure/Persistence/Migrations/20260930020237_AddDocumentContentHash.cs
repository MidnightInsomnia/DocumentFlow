using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentContentHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Documents_ContentHash",
                table: "Documents",
                column: "ContentHash",
                unique: true,
                filter: "\"ContentHash\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Documents_ContentHash",
                table: "Documents",
                sql: "\"ContentHash\" IS NULL OR \"ContentHash\" ~ '^[0-9a-f]{64}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Documents_ContentHash",
                table: "Documents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Documents_ContentHash",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Documents");
        }
    }
}
