using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRedemptionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRedemptions_PointsRewards_PointsRewardId",
                table: "UserRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_SessionId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ChatSessions");

            migrationBuilder.AlterColumn<int>(
                name: "PointsRewardId",
                table: "UserRedemptions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "MenuItemId",
                table: "UserRedemptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRedemptions_MenuItemId",
                table: "UserRedemptions",
                column: "MenuItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRedemptions_MenuItems_MenuItemId",
                table: "UserRedemptions",
                column: "MenuItemId",
                principalTable: "MenuItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRedemptions_PointsRewards_PointsRewardId",
                table: "UserRedemptions",
                column: "PointsRewardId",
                principalTable: "PointsRewards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRedemptions_MenuItems_MenuItemId",
                table: "UserRedemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRedemptions_PointsRewards_PointsRewardId",
                table: "UserRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_UserRedemptions_MenuItemId",
                table: "UserRedemptions");

            migrationBuilder.DropColumn(
                name: "MenuItemId",
                table: "UserRedemptions");

            migrationBuilder.AlterColumn<int>(
                name: "PointsRewardId",
                table: "UserRedemptions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "ChatSessions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_SessionId",
                table: "ChatSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRedemptions_PointsRewards_PointsRewardId",
                table: "UserRedemptions",
                column: "PointsRewardId",
                principalTable: "PointsRewards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
