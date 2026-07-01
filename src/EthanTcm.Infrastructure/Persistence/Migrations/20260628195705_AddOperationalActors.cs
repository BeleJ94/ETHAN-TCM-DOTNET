using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalActors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaidByUserId",
                table: "TaxPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByUserId",
                table: "TaxDeclarations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "TaxDeclarations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_PaidByUserId",
                table: "TaxPayments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_ClosedByUserId",
                table: "TaxDeclarations",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_SubmittedByUserId",
                table: "TaxDeclarations",
                column: "SubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxDeclarations_Users_ClosedByUserId",
                table: "TaxDeclarations",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxDeclarations_Users_SubmittedByUserId",
                table: "TaxDeclarations",
                column: "SubmittedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxPayments_Users_PaidByUserId",
                table: "TaxPayments",
                column: "PaidByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxDeclarations_Users_ClosedByUserId",
                table: "TaxDeclarations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxDeclarations_Users_SubmittedByUserId",
                table: "TaxDeclarations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxPayments_Users_PaidByUserId",
                table: "TaxPayments");

            migrationBuilder.DropIndex(
                name: "IX_TaxPayments_PaidByUserId",
                table: "TaxPayments");

            migrationBuilder.DropIndex(
                name: "IX_TaxDeclarations_ClosedByUserId",
                table: "TaxDeclarations");

            migrationBuilder.DropIndex(
                name: "IX_TaxDeclarations_SubmittedByUserId",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "TaxPayments");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "TaxDeclarations");
        }
    }
}
