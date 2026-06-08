using Simcag.PriceAnalysisService.Domain.Enums;

namespace Simcag.PriceAnalysisService.Domain;

/// <summary>Central thresholds for price deviation (audit guide).</summary>
public static class PriceDeviationPolicy
{
    /// <summary>Limite da coluna PostgreSQL <c>numeric(10,2)</c>.</summary>
    public const decimal MaxStoredDeviationPercent = 9999.99m;

    public const decimal WarningAbsPercent = 15m;
    public const decimal CriticalAbsPercent = 30m;
    public const decimal TrendUpPercent = 5m;
    public const decimal TrendDownPercent = -5m;
    public const decimal SafeZoneBandPercent = 0.15m;

    public static DeviationSeverity Classify(decimal? absoluteDeviationPercent)
    {
        if (!absoluteDeviationPercent.HasValue)
            return DeviationSeverity.Normal;
        var a = Math.Abs(absoluteDeviationPercent.Value);
        if (a > CriticalAbsPercent)
            return DeviationSeverity.Critical;
        if (a > WarningAbsPercent)
            return DeviationSeverity.Warning;
        return DeviationSeverity.Normal;
    }

    public static bool IsAnomalous(DeviationSeverity severity) =>
        severity is DeviationSeverity.Warning or DeviationSeverity.Critical;

    public static DeviationSeverity MaxSeverity(DeviationSeverity a, DeviationSeverity b) =>
        (int)a >= (int)b ? a : b;

    public static decimal? CapForStorage(decimal? deviationPercent)
    {
        if (!deviationPercent.HasValue)
            return null;

        var rounded = Math.Round(deviationPercent.Value, 2, MidpointRounding.AwayFromZero);
        if (rounded > MaxStoredDeviationPercent)
            return MaxStoredDeviationPercent;
        if (rounded < -MaxStoredDeviationPercent)
            return -MaxStoredDeviationPercent;
        return rounded;
    }

    public static string ToAuditString(DeviationSeverity s) => s switch
    {
        DeviationSeverity.Normal => "NORMAL",
        DeviationSeverity.Warning => "WARNING",
        DeviationSeverity.Critical => "CRITICAL",
        _ => "NORMAL"
    };
}
