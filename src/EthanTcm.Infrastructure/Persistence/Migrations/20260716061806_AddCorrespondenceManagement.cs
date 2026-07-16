using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrespondenceManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Correspondences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Confidentiality = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    CorrespondenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SenderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SenderOrganization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientOrganization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentCorrespondenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Correspondences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorrespondenceSequences",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrespondenceSequences", x => new { x.Year, x.Direction });
                });

            migrationBuilder.CreateTable(
                name: "CorrespondenceDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrespondenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrespondenceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrespondenceDocuments_Correspondences_CorrespondenceId",
                        column: x => x.CorrespondenceId,
                        principalTable: "Correspondences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CorrespondenceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrespondenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrespondenceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrespondenceHistory_Correspondences_CorrespondenceId",
                        column: x => x.CorrespondenceId,
                        principalTable: "Correspondences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceDocuments_CorrespondenceId",
                table: "CorrespondenceDocuments",
                column: "CorrespondenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceDocuments_CreatedAt",
                table: "CorrespondenceDocuments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceDocuments_CreatedByUserId_CreatedAt",
                table: "CorrespondenceDocuments",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceDocuments_UpdatedByUserId_UpdatedAt",
                table: "CorrespondenceDocuments",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceHistory_CorrespondenceId",
                table: "CorrespondenceHistory",
                column: "CorrespondenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceHistory_CreatedAt",
                table: "CorrespondenceHistory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceHistory_CreatedByUserId_CreatedAt",
                table: "CorrespondenceHistory",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceHistory_UpdatedByUserId_UpdatedAt",
                table: "CorrespondenceHistory",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_AssignedToUserId_Status",
                table: "Correspondences",
                columns: new[] { "AssignedToUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_CreatedAt",
                table: "Correspondences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_CreatedByUserId_CreatedAt",
                table: "Correspondences",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_ReferenceNumber",
                table: "Correspondences",
                column: "ReferenceNumber",
                unique: true,
                filter: "[ReferenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_Status_DueDate",
                table: "Correspondences",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Correspondences_UpdatedByUserId_UpdatedAt",
                table: "Correspondences",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorrespondenceDocuments");

            migrationBuilder.DropTable(
                name: "CorrespondenceHistory");

            migrationBuilder.DropTable(
                name: "CorrespondenceSequences");

            migrationBuilder.DropTable(
                name: "Correspondences");
        }
    }
}
