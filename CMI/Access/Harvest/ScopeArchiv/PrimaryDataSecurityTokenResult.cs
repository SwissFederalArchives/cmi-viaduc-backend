using System.Collections.Generic;

namespace CMI.Access.Harvest.ScopeArchiv
{
    public class PrimaryDataSecurityTokenResult
    {
        public PrimaryDataSecurityTokenResult()
        {
            DownloadAccessTokens = new List<string>();
            FulltextAccessTokens = new List<string>();
        }

        public List<string> DownloadAccessTokens { get; set; }
        public List<string> FulltextAccessTokens { get; set; }
    }
}