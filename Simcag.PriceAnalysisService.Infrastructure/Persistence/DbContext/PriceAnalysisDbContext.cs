using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Domain.Enums;

namespace Simcag.PriceAnalysisService.Infrastructure.Persistence.DbContext;

public class PriceAnalysisDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public PriceAnalysisDbContext(DbContextOptions<PriceAnalysisDbContext> options)
        : base(options)
    {
    }

    public DbSet<PriceAnalysis> PriceAnalyses => Set<PriceAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.OriginalPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(e => e.MarketPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.DeviationPercentage)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.HistoricalAverage)
                .HasColumnType("decimal(18,2)");

            var severity = new ValueConverter<DeviationSeverity, int>(
                v => (int)v,
                v => (DeviationSeverity)v);
            entity.Property(e => e.Severity)
                .HasConversion(severity)
                .IsRequired();

            entity.Property(e => e.AnalysisDate)
                .IsRequired();

            entity.Property(e => e.IsAnomalous)
                .IsRequired();

            entity.Property(e => e.AnalysisNotes)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.AnalysisDate);
            entity.HasIndex(e => e.IsAnomalous);
        });
    }
}