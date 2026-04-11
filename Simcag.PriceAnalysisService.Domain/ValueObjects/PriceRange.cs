namespace Simcag.PriceAnalysisService.Domain.ValueObjects;

public class PriceRange
{
    public decimal Min { get; private set; }
    public decimal Max { get; private set; }

protected PriceRange() { }

    public PriceRange(decimal min, decimal max)
    {
        if (min >= max)
            throw new ArgumentException("Min must be less than max", nameof(min));
        
        Min = min;
        Max = max;
    }

    public bool Contains(decimal price) => price >= Min && price <= Max;

    public override string ToString() => $"{Min:N2} - {Max:N2}";
}