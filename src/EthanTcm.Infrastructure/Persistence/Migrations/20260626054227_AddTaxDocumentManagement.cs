using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxDocumentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "TaxDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "TaxDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "TaxDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TaxDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TaxDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_IsDeleted",
                table: "TaxDocuments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_TaxDeclarationId_DocumentType_Version",
                table: "TaxDocuments",
                columns: new[] { "TaxDeclarationId", "DocumentType", "Version" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxDocuments_FileSize_NonNegative",
                table: "TaxDocuments",
                sql: "[FileSizeBytes] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxDocuments_Version_Positive",
                table: "TaxDocuments",
                sql: "[Version] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxDocuments_IsDeleted",
                table: "TaxDocuments");

            migrationBuilder.DropIndex(
                name: "IX_TaxDocuments_TaxDeclarationId_DocumentType_Version",
                table: "TaxDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxDocuments_FileSize_NonNegative",
                table: "TaxDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxDocuments_Version_Positive",
                table: "TaxDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TaxDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "TaxDocuments");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "TaxDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TaxDocuments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TaxDocuments");
        }
    }
}
