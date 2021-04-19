using System.Collections.Generic;
using CMI.Access.Sql.Viaduc;

namespace CMI.Web.Management.api.Data
{
    public class UserPostData : User
    {
        public List<int> AblieferndeStelleIds { get; set; }

        public string BirthdayString { get; set; }

        public string DownloadLimitDisabledUntilString { get; set; }
        public string DigitalisierungsbeschraenkungString { get; set; }
    }
}