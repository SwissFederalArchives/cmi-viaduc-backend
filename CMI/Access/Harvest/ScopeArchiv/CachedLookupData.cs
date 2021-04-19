using System;
using System.Collections.Generic;
using Serilog;

namespace CMI.Access.Harvest.ScopeArchiv
{
    public class CachedLookupData
    {
        private readonly IAISDataProvider dataProvider;

        public List<FondLink> FondsOverview;

        public CachedLookupData(IAISDataProvider dataProvider)
        {
            this.dataProvider = dataProvider;

            // Load Fonds overview data
            FondsOverview = LoadFondsOverview();
        }

        private List<FondLink> LoadFondsOverview()
        {
            try
            {
                return dataProvider.LoadFondLinks();
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error while loading the fonds list.");
                return new List<FondLink>();
            }
        }
    }
}