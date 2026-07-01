using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Login = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaxIdentificationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxFrequencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurrencesPerYear = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxFrequencies", x => x.Id);
                    table.CheckConstraint("CK_TaxFrequencies_OccurrencesPerYear_Positive", "[OccurrencesPerYear] > 0");
                });

            migrationBuilder.CreateTable(
                name: "TaxPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: true),
                    Quarter = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxPeriods", x => x.Id);
                    table.CheckConstraint("CK_TaxPeriods_DateRange", "[EndDate] >= [StartDate]");
                });

            migrationBuilder.CreateTable(
                name: "TaxTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Login = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NotificationTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OffsetDays = table.Column<int>(type: "int", nullable: false),
                    NotifyResponsible = table.Column<bool>(type: "bit", nullable: false),
                    NotifyApprover = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRules_NotificationTemplates_NotificationTemplateId",
                        column: x => x.NotificationTemplateId,
                        principalTable: "NotificationTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxObligations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxFrequencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiresPayment = table.Column<bool>(type: "bit", nullable: false),
                    RequiresSubmissionProof = table.Column<bool>(type: "bit", nullable: false),
                    RequiresPaymentProof = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LegalEntityId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxObligations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxObligations_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxObligations_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxObligations_LegalEntities_LegalEntityId1",
                        column: x => x.LegalEntityId1,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxObligations_TaxCategories_TaxCategoryId",
                        column: x => x.TaxCategoryId,
                        principalTable: "TaxCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxObligations_TaxFrequencies_TaxFrequencyId",
                        column: x => x.TaxFrequencyId,
                        principalTable: "TaxFrequencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Module = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.CheckConstraint("CK_AuditLogs_NewValue_Json", "[NewValue] IS NULL OR ISJSON([NewValue]) = 1");
                    table.CheckConstraint("CK_AuditLogs_OldValue_Json", "[OldValue] IS NULL OR ISJSON([OldValue]) = 1");
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ImportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ValidRows = table.Column<int>(type: "int", nullable: false),
                    InvalidRows = table.Column<int>(type: "int", nullable: false),
                    ErrorReportPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                    table.CheckConstraint("CK_ImportBatches_InvalidRows_NonNegative", "[InvalidRows] >= 0");
                    table.CheckConstraint("CK_ImportBatches_TotalRows_NonNegative", "[TotalRows] >= 0");
                    table.CheckConstraint("CK_ImportBatches_ValidRows_NonNegative", "[ValidRows] >= 0");
                    table.ForeignKey(
                        name: "FK_ImportBatches_Users_ImportedByUserId",
                        column: x => x.ImportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxDeclarations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentRequired = table.Column<bool>(type: "bit", nullable: false),
                    PreparedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmissionReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DelayDays = table.Column<int>(type: "int", nullable: false),
                    PenaltyRiskLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxObligationId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxPeriodId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDeclarations", x => x.Id);
                    table.CheckConstraint("CK_TaxDeclarations_DelayDays_NonNegative", "[DelayDays] >= 0");
                    table.ForeignKey(
                        name: "FK_TaxDeclarations_TaxObligations_TaxObligationId",
                        column: x => x.TaxObligationId,
                        principalTable: "TaxObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxDeclarations_TaxObligations_TaxObligationId1",
                        column: x => x.TaxObligationId1,
                        principalTable: "TaxObligations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxDeclarations_TaxPeriods_TaxPeriodId",
                        column: x => x.TaxPeriodId,
                        principalTable: "TaxPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxDeclarations_TaxPeriods_TaxPeriodId1",
                        column: x => x.TaxPeriodId1,
                        principalTable: "TaxPeriods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxDeclarations_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxDeclarations_Users_ValidatorUserId",
                        column: x => x.ValidatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxObligationResponsibles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxObligationResponsibles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxObligationResponsibles_TaxObligations_TaxObligationId",
                        column: x => x.TaxObligationId,
                        principalTable: "TaxObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaxObligationResponsibles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxScheduleRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueDay = table.Column<int>(type: "int", nullable: false),
                    DueMonth = table.Column<int>(type: "int", nullable: true),
                    MoveToNextBusinessDay = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxScheduleRules", x => x.Id);
                    table.CheckConstraint("CK_TaxScheduleRules_DueDay_Range", "[DueDay] BETWEEN 1 AND 31");
                    table.ForeignKey(
                        name: "FK_TaxScheduleRules_TaxObligations_TaxObligationId",
                        column: x => x.TaxObligationId,
                        principalTable: "TaxObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportErrors", x => x.Id);
                    table.CheckConstraint("CK_ImportErrors_RowNumber_Positive", "[RowNumber] > 0");
                    table.ForeignKey(
                        name: "FK_ImportErrors_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeclarationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclarationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclarationDocuments_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeclarationDocuments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_NotificationRules_NotificationRuleId",
                        column: x => x.NotificationRuleId,
                        principalTable: "NotificationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxDeclarationApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDeclarationApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxDeclarationApprovals_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaxDeclarationApprovals_Users_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxDocuments_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaxDocuments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxPayments", x => x.Id);
                    table.CheckConstraint("CK_TaxPayments_Amount_Positive", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_TaxPayments_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_CreatedAt",
                table: "ApplicationUsers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_CreatedByUserId_CreatedAt",
                table: "ApplicationUsers",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Email",
                table: "ApplicationUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_IsActive",
                table: "ApplicationUsers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Login",
                table: "ApplicationUsers",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_UpdatedByUserId_UpdatedAt",
                table: "ApplicationUsers",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedByUserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_Action_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "EntityName", "Action", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Module",
                table: "AuditLogs",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAt",
                table: "AuditLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UpdatedByUserId_UpdatedAt",
                table: "AuditLogs",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationDocuments_CreatedAt",
                table: "DeclarationDocuments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationDocuments_CreatedByUserId_CreatedAt",
                table: "DeclarationDocuments",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationDocuments_TaxDeclarationId",
                table: "DeclarationDocuments",
                column: "TaxDeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationDocuments_TaxDeclarationId_DocumentType",
                table: "DeclarationDocuments",
                columns: new[] { "TaxDeclarationId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationDocuments_UpdatedByUserId_UpdatedAt",
                table: "DeclarationDocuments",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationDocuments_UploadedAt",
                table: "DeclarationDocuments",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationDocuments_UploadedByUserId",
                table: "DeclarationDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CreatedAt",
                table: "Departments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CreatedByUserId_CreatedAt",
                table: "Departments",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_IsActive",
                table: "Departments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_UpdatedByUserId_UpdatedAt",
                table: "Departments",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_CreatedAt",
                table: "ImportBatches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_CreatedByUserId_CreatedAt",
                table: "ImportBatches",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_ImportedByUserId",
                table: "ImportBatches",
                column: "ImportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_Status_ImportedAt",
                table: "ImportBatches",
                columns: new[] { "Status", "ImportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_UpdatedByUserId_UpdatedAt",
                table: "ImportBatches",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportErrors_CreatedAt",
                table: "ImportErrors",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportErrors_CreatedByUserId_CreatedAt",
                table: "ImportErrors",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportErrors_ImportBatchId",
                table: "ImportErrors",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportErrors_ImportBatchId_RowNumber",
                table: "ImportErrors",
                columns: new[] { "ImportBatchId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportErrors_UpdatedByUserId_UpdatedAt",
                table: "ImportErrors",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_Code",
                table: "LegalEntities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_CreatedAt",
                table: "LegalEntities",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_CreatedByUserId_CreatedAt",
                table: "LegalEntities",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_IsActive",
                table: "LegalEntities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_UpdatedByUserId_UpdatedAt",
                table: "LegalEntities",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_CreatedAt",
                table: "NotificationLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_CreatedByUserId_CreatedAt",
                table: "NotificationLogs",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_NotificationRuleId",
                table: "NotificationLogs",
                column: "NotificationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_RecipientUserId",
                table: "NotificationLogs",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_Status_SentAt",
                table: "NotificationLogs",
                columns: new[] { "Status", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_TaxDeclarationId",
                table: "NotificationLogs",
                column: "TaxDeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_UpdatedByUserId_UpdatedAt",
                table: "NotificationLogs",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_Code",
                table: "NotificationRules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_CreatedAt",
                table: "NotificationRules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_CreatedByUserId_CreatedAt",
                table: "NotificationRules",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_NotificationTemplateId",
                table: "NotificationRules",
                column: "NotificationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_Trigger_IsActive",
                table: "NotificationRules",
                columns: new[] { "Trigger", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_UpdatedByUserId_UpdatedAt",
                table: "NotificationRules",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Code",
                table: "NotificationTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_CreatedAt",
                table: "NotificationTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_CreatedByUserId_CreatedAt",
                table: "NotificationTemplates",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_UpdatedByUserId_UpdatedAt",
                table: "NotificationTemplates",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatedAt",
                table: "Roles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatedByUserId_CreatedAt",
                table: "Roles",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsActive",
                table: "Roles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_UpdatedByUserId_UpdatedAt",
                table: "Roles",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_CreatedAt",
                table: "SystemSettings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_CreatedByUserId_CreatedAt",
                table: "SystemSettings",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedByUserId_UpdatedAt",
                table: "SystemSettings",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxCategories_Code",
                table: "TaxCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxCategories_CreatedAt",
                table: "TaxCategories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCategories_CreatedByUserId_CreatedAt",
                table: "TaxCategories",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxCategories_IsActive",
                table: "TaxCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCategories_UpdatedByUserId_UpdatedAt",
                table: "TaxCategories",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarationApprovals_ApproverUserId",
                table: "TaxDeclarationApprovals",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarationApprovals_CreatedAt",
                table: "TaxDeclarationApprovals",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarationApprovals_CreatedByUserId_CreatedAt",
                table: "TaxDeclarationApprovals",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarationApprovals_TaxDeclarationId",
                table: "TaxDeclarationApprovals",
                column: "TaxDeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarationApprovals_UpdatedByUserId_UpdatedAt",
                table: "TaxDeclarationApprovals",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_AssignedToUserId",
                table: "TaxDeclarations",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_ClosedAt",
                table: "TaxDeclarations",
                column: "ClosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_CreatedAt",
                table: "TaxDeclarations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_CreatedByUserId_CreatedAt",
                table: "TaxDeclarations",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_DueDate",
                table: "TaxDeclarations",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_Status",
                table: "TaxDeclarations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_Status_DueDate",
                table: "TaxDeclarations",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_SubmittedAt",
                table: "TaxDeclarations",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_TaxObligationId_TaxPeriodId",
                table: "TaxDeclarations",
                columns: new[] { "TaxObligationId", "TaxPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_TaxObligationId1",
                table: "TaxDeclarations",
                column: "TaxObligationId1");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_TaxPeriodId",
                table: "TaxDeclarations",
                column: "TaxPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_TaxPeriodId1",
                table: "TaxDeclarations",
                column: "TaxPeriodId1");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_UpdatedByUserId_UpdatedAt",
                table: "TaxDeclarations",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_ValidatorUserId",
                table: "TaxDeclarations",
                column: "ValidatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_CreatedAt",
                table: "TaxDocuments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_CreatedByUserId_CreatedAt",
                table: "TaxDocuments",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_TaxDeclarationId",
                table: "TaxDocuments",
                column: "TaxDeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_TaxDeclarationId_DocumentType",
                table: "TaxDocuments",
                columns: new[] { "TaxDeclarationId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_UpdatedByUserId_UpdatedAt",
                table: "TaxDocuments",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_UploadedAt",
                table: "TaxDocuments",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDocuments_UploadedByUserId",
                table: "TaxDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxFrequencies_Code",
                table: "TaxFrequencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxFrequencies_CreatedAt",
                table: "TaxFrequencies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxFrequencies_CreatedByUserId_CreatedAt",
                table: "TaxFrequencies",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxFrequencies_IsActive",
                table: "TaxFrequencies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxFrequencies_UpdatedByUserId_UpdatedAt",
                table: "TaxFrequencies",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationResponsibles_CreatedAt",
                table: "TaxObligationResponsibles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationResponsibles_CreatedByUserId_CreatedAt",
                table: "TaxObligationResponsibles",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationResponsibles_TaxObligationId_Type",
                table: "TaxObligationResponsibles",
                columns: new[] { "TaxObligationId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationResponsibles_TaxObligationId_UserId_Type",
                table: "TaxObligationResponsibles",
                columns: new[] { "TaxObligationId", "UserId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationResponsibles_UpdatedByUserId_UpdatedAt",
                table: "TaxObligationResponsibles",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligationResponsibles_UserId",
                table: "TaxObligationResponsibles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_CreatedAt",
                table: "TaxObligations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_CreatedByUserId_CreatedAt",
                table: "TaxObligations",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_DepartmentId",
                table: "TaxObligations",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_IsActive",
                table: "TaxObligations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_LegalEntityId_DepartmentId_TaxCategoryId_IsActive",
                table: "TaxObligations",
                columns: new[] { "LegalEntityId", "DepartmentId", "TaxCategoryId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_LegalEntityId_Name",
                table: "TaxObligations",
                columns: new[] { "LegalEntityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_LegalEntityId1",
                table: "TaxObligations",
                column: "LegalEntityId1");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_TaxCategoryId",
                table: "TaxObligations",
                column: "TaxCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_TaxFrequencyId",
                table: "TaxObligations",
                column: "TaxFrequencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_UpdatedByUserId_UpdatedAt",
                table: "TaxObligations",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_CreatedAt",
                table: "TaxPayments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_CreatedByUserId_CreatedAt",
                table: "TaxPayments",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_PaidAt",
                table: "TaxPayments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_PaymentReference",
                table: "TaxPayments",
                column: "PaymentReference");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_TaxDeclarationId",
                table: "TaxPayments",
                column: "TaxDeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_UpdatedByUserId_UpdatedAt",
                table: "TaxPayments",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxPeriods_CreatedAt",
                table: "TaxPeriods",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPeriods_CreatedByUserId_CreatedAt",
                table: "TaxPeriods",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxPeriods_EndDate",
                table: "TaxPeriods",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPeriods_StartDate",
                table: "TaxPeriods",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPeriods_UpdatedByUserId_UpdatedAt",
                table: "TaxPeriods",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxPeriods_Year_Month_Quarter",
                table: "TaxPeriods",
                columns: new[] { "Year", "Month", "Quarter" },
                unique: true,
                filter: "[Month] IS NOT NULL AND [Quarter] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaxScheduleRules_CreatedAt",
                table: "TaxScheduleRules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxScheduleRules_CreatedByUserId_CreatedAt",
                table: "TaxScheduleRules",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxScheduleRules_TaxObligationId",
                table: "TaxScheduleRules",
                column: "TaxObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxScheduleRules_TaxObligationId_DueMonth_DueDay",
                table: "TaxScheduleRules",
                columns: new[] { "TaxObligationId", "DueMonth", "DueDay" },
                unique: true,
                filter: "[DueMonth] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaxScheduleRules_UpdatedByUserId_UpdatedAt",
                table: "TaxScheduleRules",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxTypes_Code",
                table: "TaxTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxTypes_CreatedAt",
                table: "TaxTypes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaxTypes_CreatedByUserId_CreatedAt",
                table: "TaxTypes",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxTypes_IsActive",
                table: "TaxTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxTypes_UpdatedByUserId_UpdatedAt",
                table: "TaxTypes",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_AssignedAt",
                table: "UserRoles",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_CreatedAt",
                table: "UserRoles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_CreatedByUserId_CreatedAt",
                table: "UserRoles",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UpdatedByUserId_UpdatedAt",
                table: "UserRoles",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedByUserId_CreatedAt",
                table: "Users",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedByUserId_UpdatedAt",
                table: "Users",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUsers");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DeclarationDocuments");

            migrationBuilder.DropTable(
                name: "ImportErrors");

            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TaxDeclarationApprovals");

            migrationBuilder.DropTable(
                name: "TaxDocuments");

            migrationBuilder.DropTable(
                name: "TaxObligationResponsibles");

            migrationBuilder.DropTable(
                name: "TaxPayments");

            migrationBuilder.DropTable(
                name: "TaxScheduleRules");

            migrationBuilder.DropTable(
                name: "TaxTypes");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropTable(
                name: "NotificationRules");

            migrationBuilder.DropTable(
                name: "TaxDeclarations");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "TaxObligations");

            migrationBuilder.DropTable(
                name: "TaxPeriods");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "LegalEntities");

            migrationBuilder.DropTable(
                name: "TaxCategories");

            migrationBuilder.DropTable(
                name: "TaxFrequencies");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
