using System;
using System.Data.SqlClient;
using CMI.Access.Sql.Viaduc.AblieferndeStellen.Dto;

namespace CMI.Access.Sql.Viaduc.AblieferndeStellen
{
    public static class AblieferndeStelleTokenExtensions
    {
        public static T ToAblieferStelleToken<T>(this SqlDataReader reader, int? token = null) where T : AblieferndeStelleTokenDto, new()
        {
            var ablieferndeStelleToken = new T {TokenId = token ?? Convert.ToInt32(reader["TokenId"])};

            reader.PopulateProperties(ablieferndeStelleToken);

            return ablieferndeStelleToken;
        }
    }
}