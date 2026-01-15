using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConcreteMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PriceListContent",
                table: "Factories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceListUrl",
                table: "Factories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factories_PriceListContent",
                table: "Factories",
                column: "PriceListContent")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Factories_PriceListContent",
                table: "Factories");

            migrationBuilder.DropColumn(
                name: "PriceListContent",
                table: "Factories");

            migrationBuilder.DropColumn(
                name: "PriceListUrl",
                table: "Factories");
        }
    }
}
