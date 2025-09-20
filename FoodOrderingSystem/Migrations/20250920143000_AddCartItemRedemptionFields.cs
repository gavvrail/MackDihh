using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCartItemRedemptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add redemption fields to CartItems table
            migrationBuilder.AddColumn<bool>(
                name: "IsRedeemedWithPoints",
                table: "CartItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PointsUsed",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RedemptionCode",
                table: "CartItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove redemption fields from CartItems table
            migrationBuilder.DropColumn(
                name: "IsRedeemedWithPoints",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "PointsUsed",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "RedemptionCode",
                table: "CartItems");
        }
    }
}
