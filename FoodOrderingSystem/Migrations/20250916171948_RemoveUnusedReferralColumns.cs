using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedReferralColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop unused referral-related columns from AspNetUsers table (only if they exist)
            var columnsToCheck = new[] 
            { 
                "ReferralCode", "ReferralCount", "ReferralCredits", "ReferredBy", 
                "RewardPoints", "StudentId"
            };
            
            foreach (var column in columnsToCheck)
            {
                migrationBuilder.Sql($@"
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                              WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = '{column}')
                    BEGIN
                        DECLARE @var0 sysname;
                        SELECT @var0 = [d].[name]
                        FROM [sys].[default_constraints] [d]
                        INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                        WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'{column}');
                        IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var0 + '];');
                        ALTER TABLE [AspNetUsers] DROP COLUMN [{column}];
                    END
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the columns if migration needs to be rolled back
            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferralCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReferralCredits",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferredBy",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardPoints",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
