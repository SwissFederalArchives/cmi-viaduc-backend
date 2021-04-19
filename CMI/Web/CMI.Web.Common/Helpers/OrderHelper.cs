using System;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using Serilog;

namespace CMI.Web.Common.Helpers
{
    public class OrderHelper
    {
        public static OrderingIndexSnapshot GetOrderingIndexSnapshot(ElasticArchiveRecord entity, string unknowText = "")
        {
            var indexSnapShot = new OrderingIndexSnapshot
            {
                Darin = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.WithinInfo,
                Dossiertitel = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.Title,
                Hierarchiestufe = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.Level,
                IdentifikationDigitalesMagazin = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.PrimaryDataLink,
                Signatur = entity.ReferenceCode,
                VeId = entity.ArchiveRecordId,
                ZugaenglichkeitGemaessBga = entity.HasCustomProperty("zugänglichkeitGemässBga")
                    ? entity.CustomFields.zugänglichkeitGemässBga
                    : "",
                ZusaetzlicheInformationen = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.Extent,
                ZeitraumDossier = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.CreationPeriod?.Text,
                Schutzfristverzeichnung = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.GetSchutzfristenVerzeichnung(),
                Publikationsrechte = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.Publikationsrechte(),
                ZustaendigeStelle = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.ZuständigeStelle(),
                Aktenzeichen = !string.IsNullOrEmpty(unknowText) ? unknowText : entity.Aktenzeichen()
            };

            if (entity.Containers != null && entity.Containers.Any())
            {
                indexSnapShot.BehaeltnisCode = !string.IsNullOrEmpty(unknowText)
                    ? unknowText
                    : string.Join("; ", entity.Containers.Select(c => c.ContainerCode));
                indexSnapShot.Behaeltnistyp = !string.IsNullOrEmpty(unknowText)
                    ? unknowText
                    : string.Join("; ", entity.Containers.Select(c => c.ContainerType));
                indexSnapShot.Standort = !string.IsNullOrEmpty(unknowText)
                    ? unknowText
                    : string.Join("; ", entity.Containers.Select(c => c.ContainerLocation));
            }

            return indexSnapShot;
        }

        /// <summary>
        ///     Mappt Elastic-Daten auf ein Auftragsdetail, somit werden die zum Zeitpunkt der Bestellung aufgenommenen Index-Daten
        ///     temporär überschrieben
        ///     Damit das Admin-Backend statt "nicht-sichtbar" die korrekten, echten Daten sehen
        /// </summary>
        /// <param name="snapshot"></param>
        /// <param name="item"></param>
        public static void ApplySnapshotToDetailItem(OrderingIndexSnapshot snapshot, OrderingFlatItem item)
        {
            foreach (var snapshotProperty in typeof(OrderingIndexSnapshot).GetProperties())
            {
                try
                {
                    var itemProperty = typeof(OrderingFlatItem).GetProperty(snapshotProperty.Name);
                    if (itemProperty == null || itemProperty.PropertyType != snapshotProperty.PropertyType)
                    {
                        continue;
                    }

                    itemProperty.SetValue(item, snapshotProperty.GetValue(snapshot));
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error when applying indexsnapshot to detail item");
                }
            }

            // Ausnahmen, die sich nicht automatisch mappen lassen
            item.BehaeltnisNummer = snapshot.BehaeltnisCode;
        }
    }
}