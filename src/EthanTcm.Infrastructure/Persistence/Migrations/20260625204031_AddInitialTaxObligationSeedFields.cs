using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialTaxObligationSeedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxObligations_DepartmentId",
                table: "TaxObligations");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TaxScheduleRules",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "RawReminderText",
                table: "TaxScheduleRules",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReminderDays",
                table: "TaxScheduleRules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalDeadline",
                table: "TaxObligations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresReview",
                table: "TaxObligations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReviewReason",
                table: "TaxObligations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceNumber",
                table: "TaxObligations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxScheduleRules_IsActive",
                table: "TaxScheduleRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_DepartmentId_Name_TaxCategoryId",
                table: "TaxObligations",
                columns: new[] { "DepartmentId", "Name", "TaxCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_RequiresReview",
                table: "TaxObligations",
                column: "RequiresReview");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_SourceNumber",
                table: "TaxObligations",
                column: "SourceNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxScheduleRules_IsActive",
                table: "TaxScheduleRules");

            migrationBuilder.DropIndex(
                name: "IX_TaxObligations_DepartmentId_Name_TaxCategoryId",
                table: "TaxObligations");

            migrationBuilder.DropIndex(
                name: "IX_TaxObligations_RequiresReview",
                table: "TaxObligations");

            migrationBuilder.DropIndex(
                name: "IX_TaxObligations_SourceNumber",
                table: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TaxScheduleRules");

            migrationBuilder.DropColumn(
                name: "RawReminderText",
                table: "TaxScheduleRules");

            migrationBuilder.DropColumn(
                name: "ReminderDays",
                table: "TaxScheduleRules");

            migrationBuilder.DropColumn(
                name: "LegalDeadline",
                table: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "RequiresReview",
                table: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "ReviewReason",
                table: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "SourceNumber",
                table: "TaxObligations");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_DepartmentId",
                table: "TaxObligations",
                column: "DepartmentId");
        }
    }
}
