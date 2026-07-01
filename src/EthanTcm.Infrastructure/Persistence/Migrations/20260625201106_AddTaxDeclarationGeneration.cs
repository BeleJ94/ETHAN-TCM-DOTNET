using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxDeclarationGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxPeriods_Year_Month_Quarter",
                table: "TaxPeriods");

            migrationBuilder.AddColumn<string>(
                name: "PeriodType",
                table: "TaxPeriods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "TaxPeriods",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE TaxPeriods
                SET
                    PeriodType = CASE
                        WHEN Month IS NOT NULL THEN N'Monthly'
                        WHEN Quarter IS NOT NULL THEN N'Quarterly'
                        ELSE N'Annual'
                    END,
                    Sequence = COALESCE(Month, Quarter, 1)
                """);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReminderDate",
                table: "TaxDeclarations",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxPeriods_Year_PeriodType_Sequence",
                table: "TaxPeriods",
                columns: new[] { "Year", "PeriodType", "Sequence" },
                unique: true,
                filter: "[Sequence] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_ReminderDate",
                table: "TaxDeclarations",
                column: "ReminderDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxPeriods_Year_PeriodType_Sequence",
                table: "TaxPeriods");

            migrationBuilder.DropIndex(
                name: "IX_TaxDeclarations_ReminderDate",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PeriodType",
                table: "TaxPeriods");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "TaxPeriods");

            migrationBuilder.DropColumn(
                name: "ReminderDate",
                table: "TaxDeclarations");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPeriods_Year_Month_Quarter",
                table: "TaxPeriods",
                columns: new[] { "Year", "Month", "Quarter" },
                unique: true,
                filter: "[Month] IS NOT NULL AND [Quarter] IS NOT NULL");
        }
    }
}
