namespace Simcag.PriceAnalysisService.Domain.ValueObjects;

public class Price
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";

    private Price() { }

    public Price(decimal amount, string currency = "BRL")
    {
        if (amount <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(amount));
        
        Amount = amount;
        Currency = currency;
    }

    public override string ToString() => $"{Currency} {Amount:N2}";
}