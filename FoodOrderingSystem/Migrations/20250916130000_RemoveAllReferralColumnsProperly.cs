using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderingSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAllReferralColumnsProperly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop indexes first (only if they exist)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AspNetUsers_ReferralCode' AND object_id = OBJECT_ID('AspNetUsers'))
                    DROP INDEX [IX_AspNetUsers_ReferralCode] ON [AspNetUsers]
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AspNetUsers_StudentId' AND object_id = OBJECT_ID('AspNetUsers'))
                    DROP INDEX [IX_AspNetUsers_StudentId] ON [AspNetUsers]
            ");

            // Drop columns (only if they exist)
            var columnsToCheck = new[] 
            { 
                "ReferralCode", "ReferralCount", "ReferralCredits", "ReferredBy", 
                "RewardPoints", "StudentId", "IsStudentVerified", "StudentVerificationDate" 
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

            // InstitutionName column doesn't exist, so we skip dropping it
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add columns back (InstitutionName was never added, so we skip it)

            migrationBuilder.AddColumn<DateTime>(
                name: "StudentVerificationDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStudentVerified",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardPoints",
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
                name: "ReferralCredits",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReferralCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            // Recreate indexes
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StudentId",
                table: "AspNetUsers",
                column: "StudentId",
                unique: true,
                filter: "[StudentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ReferralCode",
                table: "AspNetUsers",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");
        }
    }
}
