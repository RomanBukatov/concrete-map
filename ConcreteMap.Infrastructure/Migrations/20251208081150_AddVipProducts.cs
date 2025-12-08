using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConcreteMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVipProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VipProducts",
                table: "Factories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VipProducts",
                table: "Factories");
        }
    }
}
