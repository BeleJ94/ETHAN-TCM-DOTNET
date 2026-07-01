using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DelegatedToUserId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DelegationEndsAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DelegationStartsAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncedAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferencesJson",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedByUserId", "Description", "IsActive", "Name", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("67bb7368-580c-41e0-a8a9-24c95f4d28f0"), "Approver", new DateTimeOffset(new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Declaration approval.", true, "Approver", null, null },
                    { new Guid("6b46396d-4056-45fd-aebb-35a5b99256bd"), "Administrator", new DateTimeOffset(new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Full system administration.", true, "Administrator", null, null },
                    { new Guid("b9d91a92-7076-4e50-ac7e-3198132ed64f"), "TaxManager", new DateTimeOffset(new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Tax obligations and declaration oversight.", true, "Tax Manager", null, null },
                    { new Guid("bc7790b2-5d9c-4579-8b39-3a0dd2eac4c2"), "Preparer", new DateTimeOffset(new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Declaration preparation.", true, "Preparer", null, null },
                    { new Guid("c3a3aa9e-25ab-4023-80e8-6398aaed017d"), "Auditor", new DateTimeOffset(new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Audit and compliance review.", true, "Auditor", null, null },
                    { new Guid("d9af8939-cd08-4e8d-a153-3269833a9c92"), "ReadOnly", new DateTimeOffset(new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Read-only access.", true, "Read Only", null, null },
                    { new Guid("e5853748-4fc9-4680-a2c0-ac643d5c9407"), "FinanceManager", new DateTimeOffset(new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Payment and finance validation.", true, "Finance Manager", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DelegatedToUserId",
                table: "Users",
                column: "DelegatedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalId",
                table: "Users",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastSyncedAt",
                table: "Users",
                column: "LastSyncedAt");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PreferencesJson_Json",
                table: "Users",
                sql: "[PreferencesJson] IS NULL OR ISJSON([PreferencesJson]) = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_DelegatedToUserId",
                table: "Users",
                column: "DelegatedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_DelegatedToUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DelegatedToUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ExternalId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LastSyncedAt",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PreferencesJson_Json",
                table: "Users");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("67bb7368-580c-41e0-a8a9-24c95f4d28f0"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6b46396d-4056-45fd-aebb-35a5b99256bd"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("b9d91a92-7076-4e50-ac7e-3198132ed64f"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("bc7790b2-5d9c-4579-8b39-3a0dd2eac4c2"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c3a3aa9e-25ab-4023-80e8-6398aaed017d"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("d9af8939-cd08-4e8d-a153-3269833a9c92"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("e5853748-4fc9-4680-a2c0-ac643d5c9407"));

            migrationBuilder.DropColumn(
                name: "DelegatedToUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DelegationEndsAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DelegationStartsAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferencesJson",
                table: "Users");
        }
    }
}
