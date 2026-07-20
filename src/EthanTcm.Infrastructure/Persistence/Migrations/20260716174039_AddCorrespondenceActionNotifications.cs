using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrespondenceActionNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorrespondenceActionNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrespondenceActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProcessingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrespondenceActionNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrespondenceActionNotifications_CorrespondenceFollowUpActions_CorrespondenceActionId",
                        column: x => x.CorrespondenceActionId,
                        principalTable: "CorrespondenceFollowUpActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CorrespondenceActionNotifications_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceActionNotifications_CorrespondenceActionId_RecipientUserId_Trigger",
                table: "CorrespondenceActionNotifications",
                columns: new[] { "CorrespondenceActionId", "RecipientUserId", "Trigger" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceActionNotifications_CreatedAt",
                table: "CorrespondenceActionNotifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceActionNotifications_CreatedByUserId_CreatedAt",
                table: "CorrespondenceActionNotifications",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceActionNotifications_RecipientUserId",
                table: "CorrespondenceActionNotifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceActionNotifications_Status_ProcessingDate",
                table: "CorrespondenceActionNotifications",
                columns: new[] { "Status", "ProcessingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrespondenceActionNotifications_UpdatedByUserId_UpdatedAt",
                table: "CorrespondenceActionNotifications",
                columns: new[] { "UpdatedByUserId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorrespondenceActionNotifications");
        }
    }
}
