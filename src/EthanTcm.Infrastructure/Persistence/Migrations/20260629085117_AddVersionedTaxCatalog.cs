using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedTaxCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalCode",
                table: "TaxObligations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogValidationStatus",
                table: "TaxObligations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Validated");

            migrationBuilder.AddColumn<string>(
                name: "ComplianceNotes",
                table: "TaxObligations",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxObligationVersionId",
                table: "TaxDeclarations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaxAllocationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Beneficiary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceRow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxAllocationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxAllocationRules_TaxObligations_TaxObligationId",
                        column: x => x.TaxObligationId,
                        principalTable: "TaxObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxAuthorities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxAuthorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxCatalogConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanonicalCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExistingValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IncomingValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceRow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCatalogConflicts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxObligationAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceRow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxObligationAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxObligationAliases_TaxObligations_TaxObligationId",
                        column: x => x.TaxObligationId,
                        principalTable: "TaxObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxRequiredDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRequiredDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxRequiredDocuments_TaxObligations_TaxObligationId",
                        column: x => x.TaxObligationId,
                        principalTable: "TaxObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxSourceReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceRow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxSourceReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxSourceReferences_TaxObligations_TaxObligationId",
                        column: x => x.TaxObligationId,
                        principalTable: "TaxObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxObligationVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FilingDeadlineRule = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaymentDeadlineRule = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RawDeadlineText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BusinessCycle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DataHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TaxAuthorityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxObligationVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxObligationVersions_TaxAuthorities_TaxAuthorityId",
                        column: x => x.TaxAuthorityId,
                        principalTable: "TaxAuthorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxObligationVersions_TaxObligations_TaxObligationId",
                        column: x => x.TaxObligationId,
                        principalTable: "TaxObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxLegalReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceRow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxLegalReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxLegalReferences_TaxObligationVersions_TaxObligationVersionId",
                        column: x => x.TaxObligationVersionId,
                        principalTable: "TaxObligationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxPenaltyRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceRow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxPenaltyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxPenaltyRules_TaxObligationVersions_TaxObligationVersionId",
                        column: x => x.TaxObligationVersionId,
                        principalTable: "TaxObligationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxProcessTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceRow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxProcessTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxProcessTemplates_TaxObligationVersions_TaxObligationVersionId",
                        column: x => x.TaxObligationVersionId,
                        principalTable: "TaxObligationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxRateRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceRow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRateRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxRateRules_TaxObligationVersions_TaxObligationVersionId",
                        column: x => x.TaxObligationVersionId,
                        principalTable: "TaxObligationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_CanonicalCode",
                table: "TaxObligations",
                column: "CanonicalCode",
                unique: true,
                filter: "[CanonicalCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_TaxObligationVersionId",
                table: "TaxDeclarations",
                column: "TaxObligationVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxAllocationRules_CreatedAt",
                table: "TaxAllocationRules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxAllocationRules_CreatedByUserId_CreatedAt",
                table: "TaxAllocationRules",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxAllocationRules_TaxObligationId_SourceName_SourceRow",
                table: "TaxAllocationRules",
                columns: new[] { "TaxObligationId", "SourceName", "SourceRow" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxAllocationRules_UpdatedByUserId_UpdatedAt",
                table: "TaxAllocationRules",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxAuthorities_Code",
                table: "TaxAuthorities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxAuthorities_CreatedAt",
                table: "TaxAuthorities",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxAuthorities_CreatedByUserId_CreatedAt",
                table: "TaxAuthorities",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxAuthorities_UpdatedByUserId_UpdatedAt",
                table: "TaxAuthorities",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxCatalogConflicts_CanonicalCode_FieldName_SourceName_SourceRow",
                table: "TaxCatalogConflicts",
                columns: new[] { "CanonicalCode", "FieldName", "SourceName", "SourceRow" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxCatalogConflicts_CreatedAt",
                table: "TaxCatalogConflicts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCatalogConflicts_CreatedByUserId_CreatedAt",
                table: "TaxCatalogConflicts",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxCatalogConflicts_Status",
                table: "TaxCatalogConflicts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCatalogConflicts_UpdatedByUserId_UpdatedAt",
                table: "TaxCatalogConflicts",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxLegalReferences_CreatedAt",
                table: "TaxLegalReferences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxLegalReferences_CreatedByUserId_CreatedAt",
                table: "TaxLegalReferences",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxLegalReferences_TaxObligationVersionId_SourceName_SourceRow",
                table: "TaxLegalReferences",
                columns: new[] { "TaxObligationVersionId", "SourceName", "SourceRow" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxLegalReferences_UpdatedByUserId_UpdatedAt",
                table: "TaxLegalReferences",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationAliases_CreatedAt",
                table: "TaxObligationAliases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationAliases_CreatedByUserId_CreatedAt",
                table: "TaxObligationAliases",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationAliases_SourceName_SourceRow_Alias",
                table: "TaxObligationAliases",
                columns: new[] { "SourceName", "SourceRow", "Alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationAliases_TaxObligationId",
                table: "TaxObligationAliases",
                column: "TaxObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationAliases_UpdatedByUserId_UpdatedAt",
                table: "TaxObligationAliases",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationVersions_CreatedAt",
                table: "TaxObligationVersions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationVersions_CreatedByUserId_CreatedAt",
                table: "TaxObligationVersions",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationVersions_TaxAuthorityId",
                table: "TaxObligationVersions",
                column: "TaxAuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationVersions_TaxObligationId_EffectiveTo",
                table: "TaxObligationVersions",
                columns: new[] { "TaxObligationId", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationVersions_TaxObligationId_VersionNumber",
                table: "TaxObligationVersions",
                columns: new[] { "TaxObligationId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationVersions_UpdatedByUserId_UpdatedAt",
                table: "TaxObligationVersions",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxPenaltyRules_CreatedAt",
                table: "TaxPenaltyRules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPenaltyRules_CreatedByUserId_CreatedAt",
                table: "TaxPenaltyRules",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxPenaltyRules_TaxObligationVersionId_SourceName_SourceRow",
                table: "TaxPenaltyRules",
                columns: new[] { "TaxObligationVersionId", "SourceName", "SourceRow" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxPenaltyRules_UpdatedByUserId_UpdatedAt",
                table: "TaxPenaltyRules",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxProcessTemplates_CreatedAt",
                table: "TaxProcessTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxProcessTemplates_CreatedByUserId_CreatedAt",
                table: "TaxProcessTemplates",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxProcessTemplates_TaxObligationVersionId_SourceName_SourceRow",
                table: "TaxProcessTemplates",
                columns: new[] { "TaxObligationVersionId", "SourceName", "SourceRow" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxProcessTemplates_UpdatedByUserId_UpdatedAt",
                table: "TaxProcessTemplates",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRateRules_CreatedAt",
                table: "TaxRateRules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRateRules_CreatedByUserId_CreatedAt",
                table: "TaxRateRules",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRateRules_TaxObligationVersionId_SourceName_SourceRow",
                table: "TaxRateRules",
                columns: new[] { "TaxObligationVersionId", "SourceName", "SourceRow" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRateRules_UpdatedByUserId_UpdatedAt",
                table: "TaxRateRules",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRequiredDocuments_CreatedAt",
                table: "TaxRequiredDocuments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRequiredDocuments_CreatedByUserId_CreatedAt",
                table: "TaxRequiredDocuments",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRequiredDocuments_TaxObligationId_DocumentType",
                table: "TaxRequiredDocuments",
                columns: new[] { "TaxObligationId", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRequiredDocuments_UpdatedByUserId_UpdatedAt",
                table: "TaxRequiredDocuments",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxSourceReferences_CreatedAt",
                table: "TaxSourceReferences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxSourceReferences_CreatedByUserId_CreatedAt",
                table: "TaxSourceReferences",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxSourceReferences_SourceName_SourceRow_TaxObligationId",
                table: "TaxSourceReferences",
                columns: new[] { "SourceName", "SourceRow", "TaxObligationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxSourceReferences_TaxObligationId",
                table: "TaxSourceReferences",
                column: "TaxObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxSourceReferences_UpdatedByUserId_UpdatedAt",
                table: "TaxSourceReferences",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_TaxDeclarations_TaxObligationVersions_TaxObligationVersionId",
                table: "TaxDeclarations",
                column: "TaxObligationVersionId",
                principalTable: "TaxObligationVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxDeclarations_TaxObligationVersions_TaxObligationVersionId",
                table: "TaxDeclarations");

            migrationBuilder.DropTable(
                name: "TaxAllocationRules");

            migrationBuilder.DropTable(
                name: "TaxCatalogConflicts");

            migrationBuilder.DropTable(
                name: "TaxLegalReferences");

            migrationBuilder.DropTable(
                name: "TaxObligationAliases");

            migrationBuilder.DropTable(
                name: "TaxPenaltyRules");

            migrationBuilder.DropTable(
                name: "TaxProcessTemplates");

            migrationBuilder.DropTable(
                name: "TaxRateRules");

            migrationBuilder.DropTable(
                name: "TaxRequiredDocuments");

            migrationBuilder.DropTable(
                name: "TaxSourceReferences");

            migrationBuilder.DropTable(
                name: "TaxObligationVersions");

            migrationBuilder.DropTable(
                name: "TaxAuthorities");

            migrationBuilder.DropIndex(
                name: "IX_TaxObligations_CanonicalCode",
                table: "TaxObligations");

            migrationBuilder.DropIndex(
                name: "IX_TaxDeclarations_TaxObligationVersionId",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "CanonicalCode",
                table: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "CatalogValidationStatus",
                table: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "ComplianceNotes",
                table: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "TaxObligationVersionId",
                table: "TaxDeclarations");
        }
    }
}
