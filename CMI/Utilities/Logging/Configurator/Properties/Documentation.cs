using CMI.Utilities.Common;

namespace CMI.Utilities.Logging.Configurator.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<LogSettings>(x => x.LogLevel, "Log-Level der ganzen Applikation");

            AddDescription<LogSettings>(x => x.OutputFolder, "Mit diesem Parameter wird festgelegt, wohin die Logdateien geschrieben werden.\n" +
                                                             "Der Text {exeName} wird durch den Namen des Excutabels ersetzt.\n");

            AddDescription<LogSettings>(x => x.SendMailOnError,
                "Mit diesem Parameter kann konfiguriert werden, bei welchen Log Fehlermeldungen ein Mail versandt wird.\n" +
                "Eine Zeile wird wie folgt aufgebaut (Teile sind durch einem Pfeil getrennt):\n" +
                "Ausschnitt des Log Textes -> Mail Titel -> Mail Empfängeradresse(n)\n\n" +
                "Falls mehr als eine Mailadresse in einer Zeile erfasst werden soll, wird zwischen den Mailadressen ein Komma eingefügt.\n" +
                "Damit eine Änderung dieses Parameters wirksam wird, muss der Service neu gestartet werden.\n" +
                "Es werden Logmeldungen mit Log Level Error und Fatal berücksichtigt.");
        }
    }
}