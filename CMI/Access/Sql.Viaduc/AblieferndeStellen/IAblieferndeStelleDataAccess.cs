using System.Collections.Generic;
using CMI.Access.Sql.Viaduc.AblieferndeStellen.Dto;

namespace CMI.Access.Sql.Viaduc.AblieferndeStellen
{
    public interface IAblieferndeStelleDataAccess
    {
        /// <summary>
        ///     Alle Ämter mit allen Zusatzinformationen zum User und Token
        /// </summary>
        /// <returns></returns>
        IEnumerable<AblieferndeStelleDetailDto> GetAllAblieferndeStelle();

        AblieferndeStelleDetailDto GetAblieferndeStelle(int ablieferndeStelleId);

        /// <summary>
        /// </summary>
        /// <param name="bezeichnung"></param>
        /// <param name="kuerzel"></param>
        /// <param name="tokenIdList">AblieferndeStelleId List</param>
        /// <returns></returns>
        int CreateAblieferndeStelle(string bezeichnung, string kuerzel, List<int> tokenIdList, List<string> kontrollstelleList, string currentUserId);

        /// <summary>
        /// </summary>
        /// <param name="ablieferndeStelleId"></param>
        /// <returns></returns>
        bool DeleteAblieferndeStelle(int[] ablieferndeStelleId);

        /// <summary>
        /// </summary>
        /// <param name="bezeichnung"></param>
        /// <param name="kuerzel"></param>
        /// <param name="tokenIdList"></param>
        /// <returns></returns>
        void UpdateAblieferndeStelle(int ablieferndeStelleId, string bezeichnung, string kuerzel, List<int> tokenIdList,
            List<string> kontrollstelleList, string currentUserId);
    }
}