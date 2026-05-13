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
        var documentLevelCategory = enriched.Category ?? string.Empty;
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

            var lineCategory = DeriveLineCategory(productName, documentLevelCategory);

            yield return new DataProcessedEvent(Guid.NewGuid(), productId, item.Amount, enriched.EnrichedAt, "ai-enrichment", lineCategory)
            {
                ProductName = productName,
                ExpenseId = expenseId,
                TenantId = tenant,
                Category = lineCategory,
                Region = string.Empty,
                SupplierId = supplierKey,
                RawDocumentId = enriched.DocumentId
            };
        }
    }

    /// <summary>
    /// A categoria do documento (ex.: "Notebook" vinda da IA) não deve substituir a categoria da linha
    /// quando a descrição já traz o bucket (ex.: "Manutenção — …").
    /// </summary>
    private static string DeriveLineCategory(string productName, string documentFallback)
    {
        var fromLine = TryExtractLeadingCategory(productName);
        if (!string.IsNullOrWhiteSpace(fromLine))
            return fromLine;

        if (LooksLikeWrongDocumentCategory(documentFallback, productName))
            return "Outros";

        return string.IsNullOrWhiteSpace(documentFallback) ? "Outros" : documentFallback.Trim();
    }

    private static string? TryExtractLeadingCategory(string productName)
    {
        var s = productName.Trim();
        if (s.Length < 3)
            return null;

        var sep = s.IndexOf('—');
        if (sep < 0)
            sep = s.IndexOf(" - ", StringComparison.Ordinal);
        if (sep <= 1 || sep > 80)
            return null;

        var prefix = s[..sep].Trim();
        return prefix.Length is < 2 or > 64 ? null : prefix;
    }

    private static bool LooksLikeWrongDocumentCategory(string documentCategory, string productName)
    {
        if (string.IsNullOrWhiteSpace(documentCategory))
            return false;

        var d = documentCategory.Trim();
        var p = productName.ToLowerInvariant();

        // IA classificou o documento como eletrónica, mas a linha é despesa de condomínio / utilidades.
        if (d.Equals("Notebook", StringComparison.OrdinalIgnoreCase)
            || d.Equals("Laptop", StringComparison.OrdinalIgnoreCase))
        {
            return p.Contains("condom", StringComparison.Ordinal)
                   || p.Contains("elevador", StringComparison.Ordinal)
                   || p.Contains("manuten", StringComparison.Ordinal)
                   || p.Contains("servi", StringComparison.Ordinal)
                   || p.Contains("utilidade", StringComparison.Ordinal)
                   || p.Contains("administrat", StringComparison.Ordinal)
                   || p.Contains("água", StringComparison.Ordinal)
                   || p.Contains("agua", StringComparison.Ordinal)
                   || p.Contains("energia", StringComparison.Ordinal)
                   || p.Contains("limpeza", StringComparison.Ordinal)
                   || p.Contains("segur", StringComparison.Ordinal)
                   || p.Contains("síndic", StringComparison.Ordinal)
                   || p.Contains("sindic", StringComparison.Ordinal)
                   || p.Contains("honor", StringComparison.Ordinal)
                   || p.Contains("fundo de reserva", StringComparison.Ordinal)
                   || p.Contains("gest", StringComparison.Ordinal);
        }

        return false;
    }
}
