using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthanTcm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalCycleNumber",
                table: "TaxDeclarations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalCycleNumber",
                table: "TaxDeclarationApprovals",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalLevel",
                table: "TaxDeclarationApprovals",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                WITH OrderedApprovals AS (
                    SELECT
                        Id,
                        TaxDeclarationId,
                        DecidedAt,
                        SUM(CASE WHEN Decision = 'Rejected' THEN 1 ELSE 0 END) OVER (
                            PARTITION BY TaxDeclarationId
                            ORDER BY DecidedAt, Id
                            ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING) AS PreviousRejections
                    FROM TaxDeclarationApprovals
                ),
                ApprovalCycles AS (
                    SELECT
                        Id,
                        TaxDeclarationId,
                        DecidedAt,
                        1 + ISNULL(PreviousRejections, 0) AS CycleNumber
                    FROM OrderedApprovals
                ),
                ApprovalLevels AS (
                    SELECT
                        Id,
                        CycleNumber,
                        ROW_NUMBER() OVER (
                            PARTITION BY TaxDeclarationId, CycleNumber
                            ORDER BY DecidedAt, Id) AS LevelNumber
                    FROM ApprovalCycles
                )
                UPDATE approvals
                SET
                    ApprovalCycleNumber = levels.CycleNumber,
                    ApprovalLevel = CASE
                        WHEN levels.LevelNumber BETWEEN 1 AND 3 THEN levels.LevelNumber
                        ELSE 3
                    END
                FROM TaxDeclarationApprovals approvals
                INNER JOIN ApprovalLevels levels ON levels.Id = approvals.Id;
                """);

            migrationBuilder.Sql(
                """
                WITH ApprovalSummary AS (
                    SELECT
                        TaxDeclarationId,
                        MAX(ApprovalCycleNumber) AS MaxCycle
                    FROM TaxDeclarationApprovals
                    GROUP BY TaxDeclarationId
                ),
                LastApproval AS (
                    SELECT TaxDeclarationId, Decision
                    FROM (
                        SELECT
                            TaxDeclarationId,
                            Decision,
                            ROW_NUMBER() OVER (
                                PARTITION BY TaxDeclarationId
                                ORDER BY DecidedAt DESC, Id DESC) AS RowNumber
                        FROM TaxDeclarationApprovals
                    ) approvals
                    WHERE RowNumber = 1
                )
                UPDATE declarations
                SET ApprovalCycleNumber = CASE
                    WHEN declarations.Status IN ('ToPrepare', 'InPreparation') AND summary.MaxCycle IS NULL THEN 0
                    WHEN declarations.Status = 'SubmittedForReview' AND lastApproval.Decision = 'Rejected' THEN summary.MaxCycle + 1
                    WHEN summary.MaxCycle IS NOT NULL THEN summary.MaxCycle
                    WHEN declarations.Status IN ('SubmittedForReview', 'ApprovedLevel1', 'ApprovedLevel2', 'ApprovedLevel3', 'ReadyForSubmission', 'Submitted', 'PaymentPending', 'Paid', 'Closed', 'Rejected') THEN 1
                    ELSE 0
                END
                FROM TaxDeclarations declarations
                LEFT JOIN ApprovalSummary summary ON summary.TaxDeclarationId = declarations.Id
                LEFT JOIN LastApproval lastApproval ON lastApproval.TaxDeclarationId = declarations.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarations_ApprovalCycleNumber",
                table: "TaxDeclarations",
                column: "ApprovalCycleNumber");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxDeclarations_ApprovalCycleNumber_NonNegative",
                table: "TaxDeclarations",
                sql: "[ApprovalCycleNumber] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarationApprovals_TaxDeclarationId_ApprovalCycleNumber_ApprovalLevel",
                table: "TaxDeclarationApprovals",
                columns: new[] { "TaxDeclarationId", "ApprovalCycleNumber", "ApprovalLevel" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxDeclarationApprovals_ApprovalCycleNumber_Positive",
                table: "TaxDeclarationApprovals",
                sql: "[ApprovalCycleNumber] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxDeclarationApprovals_ApprovalLevel_Range",
                table: "TaxDeclarationApprovals",
                sql: "[ApprovalLevel] BETWEEN 1 AND 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxDeclarations_ApprovalCycleNumber",
                table: "TaxDeclarations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxDeclarations_ApprovalCycleNumber_NonNegative",
                table: "TaxDeclarations");

            migrationBuilder.DropIndex(
                name: "IX_TaxDeclarationApprovals_TaxDeclarationId_ApprovalCycleNumber_ApprovalLevel",
                table: "TaxDeclarationApprovals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxDeclarationApprovals_ApprovalCycleNumber_Positive",
                table: "TaxDeclarationApprovals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxDeclarationApprovals_ApprovalLevel_Range",
                table: "TaxDeclarationApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovalCycleNumber",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "ApprovalCycleNumber",
                table: "TaxDeclarationApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovalLevel",
                table: "TaxDeclarationApprovals");
        }
    }
}
