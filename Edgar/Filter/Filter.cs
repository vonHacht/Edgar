using Edgar.Models;

namespace Edgar.Filter
{
    public class Filter
    {
        public static bool DaysBetweenFilings(Firm firm)
        {
            return firm.FirmTradingDays.Count == 0;
        }
    }
}
