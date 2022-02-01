using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Manager.Vecteur.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.ProxyClients.Order;
using MassTransit;
using Serilog;

namespace CMI.Manager.Vecteur
{
    public class DigitizationHelper : IDigitizationHelper
    {
        private const string noDataAvailable = "keine Angabe";
        private readonly IBus bus;
        private readonly IPublicOrder orderManagerClient;

        public DigitizationHelper(IBus bus, IPublicOrder orderManagerClient)
        {
            this.bus = bus;
            this.orderManagerClient = orderManagerClient;
        }

        public async Task<DigitalisierungsAuftrag> GetDigitalisierungsAuftrag(string archiveRecordId)
        {
            try
            {
                var externalContentClient = GetExternalContentClient();
                var response = (await externalContentClient.GetResponse<GetDigitizationOrderDataResponse>(new GetDigitizationOrderData {ArchiveRecordId = archiveRecordId})).Message;

                if (response.Result.Success)
                {
                    return response.Result.DigitizationOrder;
                }

                Log.Error("Error Message returned {response.Result.ErrorMessage}", response.Result.ErrorMessage);
                throw new InvalidOperationException(response.Result.ErrorMessage);
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception in DigitizationHelper.GetDigitalisierungsauftrag");
                throw;
            }
        }

        /// <summary>Erstellt einen DigitalisierungsAuftrag anhand der vom Benutzer gemachten Eingaben bei der Bestellung.</summary>
        /// <param name="digipoolEntry">Der Auftrag aus dem DigiPool</param>
        public async Task<DigitalisierungsAuftrag> GetManualDigitalisierungsAuftrag(DigipoolEntry digipoolEntry)
        {
            // Die Muss-Daten zunächst mit "keine Angabe" initialisieren
            var auftrag = new DigitalisierungsAuftrag
            {
                Ablieferung = new AblieferungType
                    {AblieferndeStelle = noDataAvailable, Ablieferungsnummer = noDataAvailable, AktenbildnerName = noDataAvailable},
                OrdnungsSystem = new OrdnungsSystemType {Name = noDataAvailable, Signatur = noDataAvailable, Stufe = noDataAvailable},
                Dossier = new VerzEinheitType
                    {Titel = noDataAvailable, Signatur = noDataAvailable, Entstehungszeitraum = noDataAvailable, Stufe = noDataAvailable},
                Auftragsdaten = new AuftragsdatenType {Benutzungskopie = true, BestelleinheitId = "-1"}
            };


            // Nun die Auftragsdaten mit den Benutzerangaben überschreiben, bzw. setzen
            var orderItems = await orderManagerClient.FindOrderItems(new[] {digipoolEntry.OrderItemId});
            var orderItem = orderItems.FirstOrDefault();
            if (orderItem != null)
            {
                // Verschiedene Properties nun auf die angegebenen Benutzerangaben stellen.
                auftrag.Ablieferung.Ablieferungsnummer = orderItem.Ablieferung;
                auftrag.Dossier.Aktenzeichen = orderItem.Aktenzeichen;
                auftrag.Dossier.Archivnummer = orderItem.ArchivNummer;
                if (!string.IsNullOrEmpty(orderItem.BehaeltnisNummer))
                {
                    auftrag.Dossier.Behaeltnisse = new List<BehaeltnisType>
                        {new BehaeltnisType {BehaeltnisCode = orderItem.BehaeltnisNummer, BehaeltnisTyp = noDataAvailable}};
                }

                auftrag.Dossier.Titel = orderItem.Dossiertitel;
                auftrag.Dossier.Signatur = orderItem.Signatur;
                auftrag.Dossier.Entstehungszeitraum = orderItem.ZeitraumDossier;
            }

            return auftrag;
        }

        private IRequestClient<GetDigitizationOrderData> GetExternalContentClient()
        {
            var requestTimeout = TimeSpan.FromMinutes(VecteurSettings.Default.RequestTimeoutInMinute);

            Log.Information("Getting RequestClient for {CommandName}", nameof(GetDigitizationOrderData));
            var client = bus.CreateRequestClient<GetDigitizationOrderData>(new Uri(new Uri(BusConfigurator.Uri), BusConstants.ManagementApiGetDigitizationOrderData), requestTimeout);

            return client;
        }
    }
}