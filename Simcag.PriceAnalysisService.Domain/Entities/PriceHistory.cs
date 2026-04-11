using System;
using System.ComponentModel.DataAnnotations;

namespace Simcag.PriceAnalysisService.Domain.Entities;

public class PriceHistory
{
    [Key]
    public Guid EventId { get; private set; }

    [Required]
    public string ProductId { get; private set; } = string.Empty;

    [Required]
    public decimal Price { get; private set; }

    [Required]
    public DateTime Timestamp { get; private set; }

    [Required]
    public string Source { get; private set; } = string.Empty;

    public string Market { get; private set; } = string.Empty;

    private PriceHistory() { }

    public PriceHistory(Guid eventId, string productId, decimal price, DateTime timestamp, string source, string market)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId cannot be empty", nameof(eventId));

        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ProductId is required", nameof(productId));

        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero");

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source is required", nameof(source));

        EventId = eventId;
        ProductId = productId;
        Price = price;
        Timestamp = timestamp;
        Source = source;
        Market = market ?? string.Empty;
    }
}
