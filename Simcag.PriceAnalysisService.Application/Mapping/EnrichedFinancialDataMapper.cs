using Simcag.PriceAnalysisService.Domain.Events;
using Simcag.Shared.Events;
using Simcag.Shared.Finance;

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
            var semantics = FinancialLineItemSemanticNormalizer.Repair(
                item.Description,
                item.Amount,
                item.Quantity,
                item.UnitPrice);
            var productName = string.IsNullOrWhiteSpace(semantics.CleanDescription)
                ? $"Linha {idx} documento {enriched.DocumentId}"
                : FinancialLineItemSemanticNormalizer.ToSearchQueryLabel(semantics.CleanDescription);

            var lineCategory = DeriveLineCategory(productName, documentLevelCategory);
            var effectiveQty = ResolveEffectiveQuantity(item, semantics);
            var effectiveUnit = ResolveEffectiveUnitPrice(item, semantics, effectiveQty);
            var comparableUnitPrice = ResolveComparableUnitPrice(item.Amount, effectiveQty, effectiveUnit);

            yield return new DataProcessedEvent(Guid.NewGuid(), productId, comparableUnitPrice, enriched.EnrichedAt, "ai-enrichment", lineCategory)
            {
                ProductName = productName,
                ExpenseId = expenseId,
                TenantId = tenant,
                NotifyUserId = enriched.NotifyUserId,
                Category = lineCategory,
                Region = string.Empty,
                SupplierId = supplierKey,
                RawDocumentId = enriched.DocumentId,
                Quantity = effectiveQty ?? semantics.Quantity ?? item.Quantity,
                LineTotal = item.Amount
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

    private static int? ResolveEffectiveQuantity(FinancialItem item, FinancialLineItemSemanticNormalizer.RepairResult semantics)
    {
        if (semantics.Quantity is > 1)
            return semantics.Quantity;

        if (item.Quantity is > 1)
            return item.Quantity;

        return semantics.Quantity ?? (item.Quantity is > 0 ? item.Quantity : null);
    }

    private static decimal? ResolveEffectiveUnitPrice(
        FinancialItem item,
        FinancialLineItemSemanticNormalizer.RepairResult semantics,
        int? effectiveQty)
    {
        var unit = semantics.UnitPrice ?? item.UnitPrice;
        if (unit is not > 0 || item.Amount <= 0)
            return unit;

        var unitNearLineTotal = Math.Abs(unit.Value - item.Amount) <= item.Amount * 0.02m;
        if (!unitNearLineTotal)
            return unit;

        var qty = effectiveQty ?? semantics.Quantity ?? item.Quantity;
        if (qty is > 1 || item.Quantity is > 1)
            return null;

        return null;
    }

    private static decimal ResolveComparableUnitPrice(decimal lineTotal, int? quantity, decimal? unitPrice)
    {
        if (quantity is > 1 && lineTotal > 0m)
        {
            if (unitPrice is > 0m && Math.Abs(unitPrice.Value * quantity.Value - lineTotal) <= 0.05m)
                return Math.Round(unitPrice.Value, 4, MidpointRounding.AwayFromZero);

            return Math.Round(lineTotal / quantity.Value, 4, MidpointRounding.AwayFromZero);
        }

        if (unitPrice is > 0m)
            return Math.Round(unitPrice.Value, 4, MidpointRounding.AwayFromZero);

        return lineTotal;
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
