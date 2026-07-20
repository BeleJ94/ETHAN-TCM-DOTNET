using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrespondenceFollowUpActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorrespondenceFollowUpActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrespondenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscalationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FollowUpDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WaitingFor = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompletionResult = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrespondenceFollowUpActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrespondenceFollowUpActions_Correspondences_CorrespondenceId",
                        column: x => x.CorrespondenceId,
                        principalTable: "Correspondences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CorrespondenceFollowUpActions_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrespondenceFollowUpActions_Users_EscalationUserId",
                        column: x => x.EscalationUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceFollowUpActions_AssignedToUserId_Status_DueDate",
                table: "CorrespondenceFollowUpActions",
                columns: new[] { "AssignedToUserId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceFollowUpActions_CorrespondenceId_Status",
                table: "CorrespondenceFollowUpActions",
                columns: new[] { "CorrespondenceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceFollowUpActions_CreatedAt",
                table: "CorrespondenceFollowUpActions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceFollowUpActions_CreatedByUserId_CreatedAt",
                table: "CorrespondenceFollowUpActions",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceFollowUpActions_EscalationUserId",
                table: "CorrespondenceFollowUpActions",
                column: "EscalationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceFollowUpActions_UpdatedByUserId_UpdatedAt",
                table: "CorrespondenceFollowUpActions",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorrespondenceFollowUpActions");
        }
    }
}
