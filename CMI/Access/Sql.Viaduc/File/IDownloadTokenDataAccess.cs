using System;

namespace CMI.Access.Sql.Viaduc.File
{
    public interface IDownloadTokenDataAccess
    {
        /// <summary>
        ///     Token mit Filter auf DateTime Now
        ///     - Alle nicht mehr gültigen Tokens werden entfernt (wird so gelöst um BatchJob usw. zu vermeiden)
        /// </summary>
        /// <returns></returns>
        bool CheckTokenIsValidAndClean(string token, int recordId, DownloadTokenType tokenType, string ipAdress);

        /// <summary>
        /// </summary>
        /// <param name="token"></param>
        /// <param name="ipAdress"></param>
        /// <returns></returns>
        bool CreateToken(string token, int recordId, DownloadTokenType tokenType, DateTime tokenExpiryTime, string ipAdress, string userId);

        string GetUserIdByToken(string token, int recordId, DownloadTokenType tokenType, string ipAdress);

        void CleanUpOldToken(string token, int recordId, DownloadTokenType tokenType);
    }
}