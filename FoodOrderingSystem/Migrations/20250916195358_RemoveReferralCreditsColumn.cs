using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReferralCreditsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the ReferralCredits column from AspNetUsers table (only if it exists)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                          WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'ReferralCredits')
                BEGIN
                    DECLARE @var0 sysname;
                    SELECT @var0 = [d].[name]
                    FROM [sys].[default_constraints] [d]
                    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'ReferralCredits');
                    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var0 + '];');
                    ALTER TABLE [AspNetUsers] DROP COLUMN [ReferralCredits];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the ReferralCredits column if migration needs to be rolled back
            migrationBuilder.AddColumn<int>(
                name: "ReferralCredits",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
