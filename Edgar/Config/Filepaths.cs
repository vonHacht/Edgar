using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Edgar.Config
{
    public class Filepaths
    {
        // ---------------------------- Output filenames
        public static readonly string filenameRiskpanel = "risk_panel.csv";

        public static readonly string filenameCikMatches = "cik_matches.csv";

        public static readonly string delistedMatches = "delisted_firms.csv";

        public static readonly string filterMatches = "filter.csv";

        public static readonly string logging = "edgar.log";





        // ---------------------------- Input filenames
        public static readonly string samplesFileName = "samples_2010_2023.csv";

        public static readonly string returnsCCMFileName = "returns_ccm_2010_2023.csv";

        public static readonly string returnsCRSPFileName = "returns_crsp_2010_2023.csv";

    }
}
