using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using MassTransit;

namespace CMI.Manager.Order
{
    /// <summary>
    ///     Die Klasse Portfolio speichert während der Statusübergänge EMail, welche später, wenn
    ///     alle Statusübergänge erfolgreich waren, versendet werden.
    /// </summary>
    public class EMailPortfolio : ISendable
    {
        private readonly Dictionary<string, object> pendingEMails = new Dictionary<string, object>();


        async Task ISendable.Send(IBus bus)
        {
            foreach (ISendable email in pendingEMails.Values)
            {
                await email.Send(bus);
            }
        }

        /// <summary>
        ///     Speichert eine Mail im Portfolio. Diese Methode soll verwendet werden,
        ///     wenn nicht beabsichtigt wird, das Mail später noch mit Daten zu erweitern.
        /// </summary>
        /// <param name="template">Ein Template auf Basis von EmailTemplateSetting.</param>
        /// <param name="data"></param>
        public void AddFinishedMail<T>(ExpandoObject data) where T : EmailTemplate, new()
        {
            var key = Guid.NewGuid().ToString("N");
            pendingEMails.Add(key, new EMail<T>(data, key));
        }

        /// <summary>
        ///     Speichert eine Mail im Portfolio, welche später noch erweitert werden kann.
        ///     Zu Beispiel werde alle Ves einer Transaktion im EMail aufgelistet.
        /// </summary>
        /// <param name="key">Ein Schlüssel, z.B. 'Eingangsbestätigung', oder 'Notifikation_Freigabemanager' </param>
        /// <param name="template">Ein Template auf Basis von EmailTemplateSetting.</param>
        /// <param name="data"></param>
        public void BeginUnfinishedMail<T>(string key, ExpandoObject data) where T : EmailTemplate, new()
        {
            pendingEMails.Add(key, new EMail<T>(data, key));
        }

        /// <summary>
        ///     Holt das Expando Objekt eines mit der Funktion BeginUnfinished Mail erstellten EMails
        ///     damit der Caller es dann mit weiteren Daten anreichern kann.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ExpandoObject GetUnfinishedMailData<T>(string key) where T : EmailTemplate, new()
        {
            object email;
            if (pendingEMails.TryGetValue(key, out email))
            {
                var emailTyped = (EMail<T>) email;
                return emailTyped.ExpandoObject;
            }

            return null;
        }
    }
}