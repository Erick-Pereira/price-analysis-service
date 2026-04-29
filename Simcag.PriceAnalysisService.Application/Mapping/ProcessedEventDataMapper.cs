using System;
using System.Text.Json;
using Simcag.Shared.Events;

namespace Simcag.PriceAnalysisService.Application.Mapping;

public sealed class ProcessedEventAudit
{
    public Guid? ExpenseId { get; init; }
    public string? TenantId { get; init; }
    public string? Category { get; init; }
    public string? Region { get; init; }
    public string? SupplierId { get; init; }
}

public static class ProcessedEventDataMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ProcessedEventAudit MapAudit(PriceDataProcessedEvent incoming)
    {
        if (incoming.EventData is null)
            return new ProcessedEventAudit();

        try
        {
            var json = JsonSerializer.Serialize(incoming.EventData, JsonOptions);
            var dto = JsonSerializer.Deserialize<EventDataShape>(json, JsonOptions);
            if (dto is null)
                return new ProcessedEventAudit();

            Guid? expenseId = null;
            if (!string.IsNullOrWhiteSpace(dto.ExpenseId) && Guid.TryParse(dto.ExpenseId, out var g2))
                expenseId = g2;

            return new ProcessedEventAudit
            {
                ExpenseId = expenseId,
                TenantId = string.IsNullOrWhiteSpace(dto.TenantId) ? null : dto.TenantId,
                Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category,
                Region = string.IsNullOrWhiteSpace(dto.Region) ? null : dto.Region,
                SupplierId = string.IsNullOrWhiteSpace(dto.SupplierId) ? null : dto.SupplierId
            };
        }
        catch
        {
            return new ProcessedEventAudit();
        }
    }

    private sealed class EventDataShape
    {
        public string? ExpenseId { get; set; }
        public string? TenantId { get; set; }
        public string? Category { get; set; }
        public string? Region { get; set; }
        public string? SupplierId { get; set; }
    }
}
