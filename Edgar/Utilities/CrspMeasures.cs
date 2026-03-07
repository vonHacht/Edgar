using Edgar.Models;

public static class CrspMeasures
{
    /// <summary>
    /// Computes buy-and-hold return from the first trading day on/after startDate
    /// through the last trading day on/before endDate.
    ///
    /// Uses CRSP daily RET and compounds:
    /// Product(1 + ret_t) - 1
    /// </summary>
    public static double? ComputeBuyAndHoldReturn(
        CrspData crsp,
        int permno,
        DateTime startDate,
        DateTime endDate)
    {
        if (endDate < startDate)
            return null;

        var days = GetWindowDays(crsp, permno, startDate, endDate);

        if (days.Count == 0)
            return null;

        double wealth = 1.0;
        bool hasAtLeastOneReturn = false;

        foreach (var day in days)
        {
            if (!day.Ret.HasValue || double.IsNaN(day.Ret.Value))
                continue;

            wealth *= (1.0 + day.Ret.Value);
            hasAtLeastOneReturn = true;
        }

        if (!hasAtLeastOneReturn)
            return null;

        return wealth - 1.0;
    }

    /// <summary>
    /// Computes daily return volatility over the window from the first trading day
    /// on/after startDate through the last trading day on/before endDate.
    ///
    /// Returns the sample standard deviation of CRSP daily RET.
    /// If annualize = true, multiplies by sqrt(252).
    /// </summary>
    public static double? ComputeVolatility(
        CrspData crsp,
        int permno,
        DateTime startDate,
        DateTime endDate,
        bool annualize = false)
    {
        if (endDate < startDate)
            return null;

        var days = GetWindowDays(crsp, permno, startDate, endDate);

        var returns = days
            .Where(d => d.Ret.HasValue && !double.IsNaN(d.Ret.Value))
            .Select(d => d.Ret!.Value)
            .ToList();

        if (returns.Count < 2)
            return null;

        double mean = returns.Average();

        double sumSq = 0.0;
        foreach (var r in returns)
        {
            double diff = r - mean;
            sumSq += diff * diff;
        }

        double variance = sumSq / (returns.Count - 1);
        double vol = Math.Sqrt(variance);

        if (annualize)
            vol *= Math.Sqrt(252.0);

        return vol;
    }

    /// <summary>
    /// Gets all trading days for permno within [startDate, endDate], inclusive.
    /// Pulls from all relevant years in CrspData.
    /// Assumes lists were sorted via SortAll().
    /// </summary>
    private static List<FirmTradingDay> GetWindowDays(
        CrspData crsp,
        int permno,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new List<FirmTradingDay>();

        for (int year = startDate.Year; year <= endDate.Year; year++)
        {
            var days = crsp.GetDays(year, permno);
            if (days.Count == 0)
                continue;

            foreach (var day in days)
            {
                if (day.Date < startDate)
                    continue;

                if (day.Date > endDate)
                    break;

                result.Add(day);
            }
        }

        return result;
    }
}
