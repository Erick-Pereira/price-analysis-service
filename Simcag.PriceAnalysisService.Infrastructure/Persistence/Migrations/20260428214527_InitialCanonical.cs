using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simcag.PriceAnalysisService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCanonical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MarketPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    HistoricalAverage = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DeviationPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAnomalous = table.Column<bool>(type: "boolean", nullable: false),
                    AnalysisNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAnalyses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAnalyses_AnalysisDate",
                table: "PriceAnalyses",
                column: "AnalysisDate");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAnalyses_IsAnomalous",
                table: "PriceAnalyses",
                column: "IsAnomalous");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAnalyses_ProductId",
                table: "PriceAnalyses",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceAnalyses");
        }
    }
}
