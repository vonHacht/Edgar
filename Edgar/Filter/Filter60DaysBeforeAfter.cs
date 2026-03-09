using Edgar.Models;

namespace Edgar.Filter
{
    public static class FilterFunctions
    {
        public static string Filter60DaysBeforeAfter(int permno, int year, CrspData crspData)
        {
            // At least 60 days of returns and trading
            // volume in the year before and after the filing date

            int yearBefore = year - 1;
            int yearAfter = year + 1;

            if (crspData.ContainsKey(yearBefore) && crspData.ContainsKey(yearAfter))
            {
                List<FirmTradingDay> daysBefore = crspData
                    .GetDays(yearBefore, permno)
                    .TakeLast(60)
                    .ToList();

                List<FirmTradingDay> daysAfter = crspData
                    .GetDays(yearAfter, permno)
                    .Take(60)
                    .ToList();

                if (daysBefore.Count < 60 || daysAfter.Count < 60)
                    return "Not enough days before or after";

                for (int i = 0; i < 60; i++)
                {
                    if (daysBefore[i].Ret == null || daysBefore[i].Volume == null)
                        return "Days before missing return or volume";
                    if (daysAfter[i].Ret == null || daysAfter[i].Volume == null)
                        return "Days after missing return or volume";
                }

            }
            else
                return "Missing data for year before or after";

            return "";
        }
    }
}
