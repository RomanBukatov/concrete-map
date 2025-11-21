using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConcreteMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Factories_Comment",
                table: "Factories",
                column: "Comment")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Factories_Name",
                table: "Factories",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Factories_ProductCategories",
                table: "Factories",
                column: "ProductCategories")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Factories_Comment",
                table: "Factories");

            migrationBuilder.DropIndex(
                name: "IX_Factories_Name",
                table: "Factories");

            migrationBuilder.DropIndex(
                name: "IX_Factories_ProductCategories",
                table: "Factories");
        }
    }
}
