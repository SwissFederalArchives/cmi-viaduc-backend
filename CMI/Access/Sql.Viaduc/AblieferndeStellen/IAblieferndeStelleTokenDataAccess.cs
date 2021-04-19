using System.Collections.Generic;
using CMI.Access.Sql.Viaduc.AblieferndeStellen.Dto;

namespace CMI.Access.Sql.Viaduc.AblieferndeStellen
{
    public interface IAblieferndeStelleTokenDataAccess
    {
        /// <summary>
        ///     Alle Tokens mit den dazugehörigen Ämter bzw. verlinkten Ämter
        /// </summary>
        /// <returns></returns>
        IEnumerable<AmtTokenDto> GetAllTokens();

        /// <summary>
        ///     Token mit den dazugehörigen Ämter bzw. verlinkten Ämter
        /// </summary>
        /// <returns></returns>
        AmtTokenDto GetToken(int tokenId);

        /// <summary>
        /// </summary>
        /// <param name="token"></param>
        /// <param name="bezeichnung"></param>
        /// <returns></returns>
        AmtTokenDto CreateToken(string token, string bezeichnung);

        /// <summary>
        /// </summary>
        /// <param name="tokenIds"></param>
        /// <returns></returns>
        void DeleteToken(int[] tokenIds);

        /// <summary>
        /// </summary>
        /// <param name="tokenId">Token Wert vor dem editieren</param>
        /// <param name="token">Neuer Wert - falls dieser aktualisiert werden soll</param>
        /// <param name="bezeichnung"></param>
        /// <returns></returns>
        void UpdateToken(int tokenId, string token, string bezeichnung);
    }
}