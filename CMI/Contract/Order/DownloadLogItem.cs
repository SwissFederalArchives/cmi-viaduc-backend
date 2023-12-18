using System;

namespace CMI.Contract.Order;

public class DownloadLogItem
{
    public string Token { get; set; }
    public string UserId { get; set; }
    public string UserTokens { get; set; }
    public string Vorgang { get; set; }
    public string Signatur { get; set; }
    public string Titel { get; set; }
    public string Zeitraum { get; set; }
    public string Schutzfrist { get; set; }
    public DateTime DatumVorgang { get; set; }
    public DateTime DatumErstellungToken { get; set; }

}