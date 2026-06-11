namespace Simcag.PriceAnalysisService.Application.Benchmarking;

/// <summary>Mirrors market-data tiered plausibility for cache validation.</summary>
internal static class MarketDeclaredPlausibility
{
    private const decimal MaxRatioToDeclared = 5.0m;

    public static bool IsPlausible(decimal candidatePrice, decimal declaredReferenceBrl)
    {
        if (declaredReferenceBrl <= 0.01m || candidatePrice <= 0.01m)
            return true;

        var ratio = candidatePrice / declaredReferenceBrl;
        var minRatio = ResolveMinRatio(declaredReferenceBrl);
        return ratio >= minRatio && ratio <= MaxRatioToDeclared;
    }

    public static string DeclaredPriceBucket(decimal declaredReferenceBrl)
    {
        if (declaredReferenceBrl <= 0.01m)
            return "none";

        if (declaredReferenceBrl >= 2000m)
            return "hi";
        if (declaredReferenceBrl >= 500m)
            return "mid";
        if (declaredReferenceBrl >= 50m)
            return "lo";
        return "micro";
    }

    private static decimal ResolveMinRatio(decimal declaredReferenceBrl)
    {
        if (declaredReferenceBrl >= 2000m)
            return 0.20m;
        if (declaredReferenceBrl >= 500m)
            return 0.15m;
        return 0.05m;
    }
}
