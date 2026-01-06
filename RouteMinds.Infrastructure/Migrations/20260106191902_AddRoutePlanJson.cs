using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouteMinds.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutePlanJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoutePlanJson",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoutePlanJson",
                table: "Orders");
        }
    }
}
