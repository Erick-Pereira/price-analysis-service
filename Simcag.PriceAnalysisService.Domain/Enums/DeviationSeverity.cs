namespace Simcag.PriceAnalysisService.Domain.Enums;

/// <summary>Audit-oriented severity from absolute deviation % vs market/historical reference.</summary>
public enum DeviationSeverity
{
    Normal = 0,
    Warning = 1,
    Critical = 2
}
