namespace CMI.Access.Sql.Viaduc.File
{
    public interface IDownloadLogDataAccess
    {
        /// <summary>
        ///     Schreibt ins DowloadLog einen Eintrag, welcher bezeugt, dass ein Token generiert wurde für einen bestimmten
        ///     Benutzer und eine bestimmte VE.
        /// </summary>
        /// <param name="token">Das Token</param>
        /// <param name="userId">ID des Benutzers</param>
        /// <param name="userTokens">Tokens des Benutzers: Ö1, Ö2, etc.</param>
        /// <param name="signatur">Signatur der VE</param>
        /// <param name="titel">Titel der VE</param>
        /// <param name="schutzfrist">Schutzfrist der VE</param>
        void LogTokenGeneration(string token, string userId, string userTokens, string signatur, string titel, string schutzfrist);


        /// <summary>
        ///     Ergänzt den Logeintrag mit dem aktuellen Datum und schreibt den Vorgang auf den Log-Datensatz.
        ///     Dies bezeugt, dass nicht nur ein Token erzeugt wurde, sondern dass der Download tatsächlich gestartet wurde.
        ///     Bevor diese Funktion aufgerufen wird, muss die Funktion LogTokenGeneration aufgerufen werden mit dem gleichen Wert
        ///     für den Parameter token.
        /// </summary>
        /// <param name="vorgang"></param>
        void LogVorgang(string token, string vorgang);
    }
}