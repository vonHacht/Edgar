using System;
using System.Collections.Generic;
using System.Text;

using Edgar.Models;

namespace Edgar.Filter
{
    public class Filter
    {
        public static bool MatchingCIK(Firm firm)
        {
            return firm.CRSPSmall.Count > 0;
        }



    }
}
