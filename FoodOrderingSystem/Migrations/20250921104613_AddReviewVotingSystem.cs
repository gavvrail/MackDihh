using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewVotingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add CartItem columns only if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                              WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'IsRedeemedWithPoints')
                    ALTER TABLE [CartItems] ADD [IsRedeemedWithPoints] bit NOT NULL DEFAULT CAST(0 AS bit);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                              WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'PointsUsed')
                    ALTER TABLE [CartItems] ADD [PointsUsed] int NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                              WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'RedemptionCode')
                    ALTER TABLE [CartItems] ADD [RedemptionCode] nvarchar(max) NULL;
            ");

            migrationBuilder.CreateTable(
                name: "ReviewVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VoteType = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewVotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewVotes_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewVotes_ReviewId",
                table: "ReviewVotes",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewVotes_UserId_ReviewId",
                table: "ReviewVotes",
                columns: new[] { "UserId", "ReviewId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewVotes");

            // Drop CartItem columns only if they exist
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                          WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'IsRedeemedWithPoints')
                    ALTER TABLE [CartItems] DROP COLUMN [IsRedeemedWithPoints];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                          WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'PointsUsed')
                    ALTER TABLE [CartItems] DROP COLUMN [PointsUsed];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                          WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'RedemptionCode')
                    ALTER TABLE [CartItems] DROP COLUMN [RedemptionCode];
            ");
        }
    }
}
