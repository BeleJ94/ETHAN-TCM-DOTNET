using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrespondenceReferentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessReference",
                table: "Correspondences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrespondentOrganizationId",
                table: "Correspondences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InternalDepartmentId",
                table: "Correspondences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CorrespondenceOrganizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrespondenceOrganizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_BusinessReference",
                table: "Correspondences",
                column: "BusinessReference");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_CorrespondentOrganizationId",
                table: "Correspondences",
                column: "CorrespondentOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_InternalDepartmentId",
                table: "Correspondences",
                column: "InternalDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceOrganizations_Code",
                table: "CorrespondenceOrganizations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceOrganizations_CreatedAt",
                table: "CorrespondenceOrganizations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceOrganizations_CreatedByUserId_CreatedAt",
                table: "CorrespondenceOrganizations",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceOrganizations_IsActive_Name",
                table: "CorrespondenceOrganizations",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceOrganizations_UpdatedByUserId_UpdatedAt",
                table: "CorrespondenceOrganizations",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Correspondences_CorrespondenceOrganizations_CorrespondentOrganizationId",
                table: "Correspondences",
                column: "CorrespondentOrganizationId",
                principalTable: "CorrespondenceOrganizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Correspondences_Departments_InternalDepartmentId",
                table: "Correspondences",
                column: "InternalDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Correspondences_CorrespondenceOrganizations_CorrespondentOrganizationId",
                table: "Correspondences");

            migrationBuilder.DropForeignKey(
                name: "FK_Correspondences_Departments_InternalDepartmentId",
                table: "Correspondences");

            migrationBuilder.DropTable(
                name: "CorrespondenceOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_Correspondences_BusinessReference",
                table: "Correspondences");

            migrationBuilder.DropIndex(
                name: "IX_Correspondences_CorrespondentOrganizationId",
                table: "Correspondences");

            migrationBuilder.DropIndex(
                name: "IX_Correspondences_InternalDepartmentId",
                table: "Correspondences");

            migrationBuilder.DropColumn(
                name: "BusinessReference",
                table: "Correspondences");

            migrationBuilder.DropColumn(
                name: "CorrespondentOrganizationId",
                table: "Correspondences");

            migrationBuilder.DropColumn(
                name: "InternalDepartmentId",
                table: "Correspondences");
        }
    }
}
