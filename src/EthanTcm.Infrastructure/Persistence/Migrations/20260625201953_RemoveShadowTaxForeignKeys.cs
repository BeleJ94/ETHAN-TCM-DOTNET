using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShadowTaxForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxDeclarations_TaxObligations_TaxObligationId1",
                table: "TaxDeclarations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxDeclarations_TaxPeriods_TaxPeriodId1",
                table: "TaxDeclarations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxObligations_LegalEntities_LegalEntityId1",
                table: "TaxObligations");

            migrationBuilder.DropIndex(
                name: "IX_TaxObligations_LegalEntityId1",
                table: "TaxObligations");

            migrationBuilder.DropIndex(
                name: "IX_TaxDeclarations_TaxObligationId1",
                table: "TaxDeclarations");

            migrationBuilder.DropIndex(
                name: "IX_TaxDeclarations_TaxPeriodId1",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "LegalEntityId1",
                table: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "TaxObligationId1",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "TaxPeriodId1",
                table: "TaxDeclarations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LegalEntityId1",
                table: "TaxObligations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxObligationId1",
                table: "TaxDeclarations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxPeriodId1",
                table: "TaxDeclarations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_LegalEntityId1",
                table: "TaxObligations",
                column: "LegalEntityId1");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_TaxObligationId1",
                table: "TaxDeclarations",
                column: "TaxObligationId1");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_TaxPeriodId1",
                table: "TaxDeclarations",
                column: "TaxPeriodId1");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxDeclarations_TaxObligations_TaxObligationId1",
                table: "TaxDeclarations",
                column: "TaxObligationId1",
                principalTable: "TaxObligations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxDeclarations_TaxPeriods_TaxPeriodId1",
                table: "TaxDeclarations",
                column: "TaxPeriodId1",
                principalTable: "TaxPeriods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxObligations_LegalEntities_LegalEntityId1",
                table: "TaxObligations",
                column: "LegalEntityId1",
                principalTable: "LegalEntities",
                principalColumn: "Id");
        }
    }
}
