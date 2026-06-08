using Simcag.PriceAnalysisService.Application.Mapping;
using Simcag.PriceAnalysisService.Domain;
using Simcag.Shared.Events;

namespace Simcag.PriceAnalysisService.Tests;

public sealed class EnrichedFinancialDataMapperTests
{
    [Fact]
    public void ToDataProcessedEvents_UsesUnitPrice_WhenQuantityGreaterThanOne()
    {
        var enriched = new EnrichedFinancialDataEvent
        {
            EventId = Guid.NewGuid(),
            DocumentId = "doc-1",
            EnrichedAt = DateTime.UtcNow,
            Supplier = new SupplierInfo { NormalizedName = "Fornecedor" },
            Items =
            [
                new FinancialItem
                {
                    Description = "Camera IP Full HD 2MP",
                    Amount = 10680m,
                    Quantity = 12,
                    UnitPrice = 890m
                }
            ]
        };

        var row = EnrichedFinancialDataMapper.ToDataProcessedEvents(enriched).Single();

        Assert.Equal(890m, row.Price);
        Assert.Equal(10680m, row.LineTotal);
        Assert.Equal(12, row.Quantity);
    }

    [Fact]
    public void ToDataProcessedEvents_PropagatesNotifyUserId()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var enriched = new EnrichedFinancialDataEvent
        {
            EventId = Guid.NewGuid(),
            DocumentId = "doc-2",
            TenantId = "9a3f8805-4885-4742-8aff-0d0ac89eda96",
            NotifyUserId = userId,
            EnrichedAt = DateTime.UtcNow,
            Supplier = new SupplierInfo { NormalizedName = "Fornecedor" },
            Items = [new FinancialItem { Description = "Serviço", Amount = 100m }]
        };

        var row = EnrichedFinancialDataMapper.ToDataProcessedEvents(enriched).Single();
        Assert.Equal(userId, row.NotifyUserId);
    }

    [Fact]
    public void CapForStorage_ClampsExtremeDeviation()
    {
        var capped = PriceDeviationPolicy.CapForStorage(10582.4m);
        Assert.Equal(9999.99m, capped);
    }
}
