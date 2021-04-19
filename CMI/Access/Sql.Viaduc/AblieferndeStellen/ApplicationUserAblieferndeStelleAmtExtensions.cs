using System.Data.SqlClient;
using CMI.Access.Sql.Viaduc.AblieferndeStellen.Dto;

namespace CMI.Access.Sql.Viaduc.AblieferndeStellen
{
    public static class ApplicationUserAblieferndeStelleAmtExtensions
    {
        public static ApplicationUserAblieferndeStelleAmtDto ToApplicationUserAblieferndeStelleAmt(
            this SqlDataReader reader)
        {
            var ablieferndeStelle = new ApplicationUserAblieferndeStelleAmtDto();
            reader.PopulateProperties(ablieferndeStelle);

            return ablieferndeStelle;
        }
    }
}