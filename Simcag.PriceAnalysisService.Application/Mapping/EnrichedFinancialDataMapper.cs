using Simcag.PriceAnalysisService.Domain.Events;
using Simcag.Shared.Events;

namespace Simcag.PriceAnalysisService.Application.Mapping;

/// <summary>Mapeia <see cref="EnrichedFinancialDataEvent"/> (AI Service) para o DTO interno do pipeline de análise.</summary>
public static class EnrichedFinancialDataMapper
{
    public static IEnumerable<DataProcessedEvent> ToDataProcessedEvents(EnrichedFinancialDataEvent enriched)
    {
        ArgumentNullException.ThrowIfNull(enriched);

        Guid? expenseId = null;
        if (!string.IsNullOrWhiteSpace(enriched.ExpenseId) && Guid.TryParse(enriched.ExpenseId, out var ex))
            expenseId = ex;

        var tenant = enriched.TenantId ?? string.Empty;
        var categoryOrMarket = enriched.Category ?? string.Empty;
        var supplierKey = string.IsNullOrWhiteSpace(enriched.Supplier.TaxId)
            ? enriched.Supplier.NormalizedName.Trim()
            : enriched.Supplier.TaxId!.Trim();

        var idx = 0;
        foreach (var item in enriched.Items)
        {
            if (item.Amount <= 0m)
                continue;

            idx++;
            var productId = $"{enriched.DocumentId}:{idx}";
            var productName = string.IsNullOrWhiteSpace(item.Description)
                ? $"Linha {idx} documento {enriched.DocumentId}"
                : item.Description.Trim();

            yield return new DataProcessedEvent(Guid.NewGuid(), productId, item.Amount, enriched.EnrichedAt, "ai-enrichment", categoryOrMarket)
            {
                ProductName = productName,
                ExpenseId = expenseId,
                TenantId = tenant,
                Category = categoryOrMarket,
                Region = string.Empty,
                SupplierId = supplierKey,
                RawDocumentId = enriched.DocumentId
            };
        }
    }
}
