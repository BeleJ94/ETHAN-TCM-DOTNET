using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_TaxDeclarationId_DocumentType_IsDeleted_UploadedAt",
                table: "TaxDocuments",
                columns: new[] { "TaxDeclarationId", "DocumentType", "IsDeleted", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_AssignedToUserId_Status_DueDate",
                table: "TaxDeclarations",
                columns: new[] { "AssignedToUserId", "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxDocuments_TaxDeclarationId_DocumentType_IsDeleted_UploadedAt",
                table: "TaxDocuments");

            migrationBuilder.DropIndex(
                name: "IX_TaxDeclarations_AssignedToUserId_Status_DueDate",
                table: "TaxDeclarations");
        }
    }
}
