using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderCancellations_AspNetUsers_UserId",
                table: "OrderCancellations");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderCancellations_Orders_OrderId",
                table: "OrderCancellations");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderCancellations_AspNetUsers_UserId",
                table: "OrderCancellations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderCancellations_Orders_OrderId",
                table: "OrderCancellations",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderCancellations_AspNetUsers_UserId",
                table: "OrderCancellations");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderCancellations_Orders_OrderId",
                table: "OrderCancellations");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderCancellations_AspNetUsers_UserId",
                table: "OrderCancellations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderCancellations_Orders_OrderId",
                table: "OrderCancellations",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
