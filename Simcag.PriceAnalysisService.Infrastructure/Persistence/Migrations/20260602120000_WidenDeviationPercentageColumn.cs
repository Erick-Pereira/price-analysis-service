using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simcag.PriceAnalysisService.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class WidenDeviationPercentageColumn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "DeviationPercentage",
            table: "PriceAnalyses",
            type: "numeric(10,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(5,2)",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "DeviationPercentage",
            table: "PriceAnalyses",
            type: "numeric(5,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(10,2)",
            oldNullable: true);
    }
}
