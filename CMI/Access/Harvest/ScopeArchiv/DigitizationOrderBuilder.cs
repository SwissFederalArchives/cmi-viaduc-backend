using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using Serilog;

namespace CMI.Access.Harvest.ScopeArchiv
{
    public class DigitizationOrderBuilder
    {
        public const string NoDataAvailable = "keine Angabe";
        private const string DossierLevelIdentifier = "Dossier";
        private const string SubFondsLevelIdentifier = "Teilbestand";
        private const string FondsLevelIdentifier = "Bestand";
        private const string SerieLevelIdentifier = "Serie";
        private readonly ConcurrentDictionary<long, List<VerzEinheitKurzType>> containerContentCache;
        private readonly IAISDataProvider dataProvider;
        private readonly ArchiveRecordBuilder recordBuilder;
        private readonly SipDateBuilder sipDateBuilder;

        public DigitizationOrderBuilder(IAISDataProvider dataProvider, ArchiveRecordBuilder recordBuilder, SipDateBuilder sipDateBuilder)
        {
            this.dataProvider = dataProvider;
            this.recordBuilder = recordBuilder;
            this.sipDateBuilder = sipDateBuilder;
            containerContentCache = new ConcurrentDictionary<long, List<VerzEinheitKurzType>>();
        }

        /// <summary>
        ///     Builds the digitization order from a given record id.
        ///     The record id should be from a unit of description which is either a document or dossier.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>DigitalisierungsAuftrag.</returns>
        public DigitalisierungsAuftrag Build(string recordId)
        {
            var result = new DigitalisierungsAuftrag();

            // clear cache 
            containerContentCache.Clear();

            // Get lots of metadata we need for processing
            var archiveRecord = recordBuilder.Build(recordId);

            if (archiveRecord != null)
            {
                // Accession Data
                var t1 = Task.Factory.StartNew(() => { result.Ablieferung = GetAccessionData(recordId, archiveRecord); });

                var t2 = Task.Factory.StartNew(() =>
                {
                    // Ordering position Data
                    result.OrdnungsSystem = GetOrderingPositionData(archiveRecord);
                });

                var t3 = Task.Factory.StartNew(() =>
                {
                    // Archive record data
                    result.Dossier = GetDossierData(archiveRecord);
                });

                Task.WaitAll(t1, t2, t3);

                // The basic order data
                result.Auftragsdaten = GetOrderData(recordId, result);
            }
            else
            {
                result.Ablieferung = new AblieferungType
                    {AblieferndeStelle = NoDataAvailable, Ablieferungsnummer = NoDataAvailable, AktenbildnerName = NoDataAvailable};
                result.OrdnungsSystem = new OrdnungsSystemType {Name = NoDataAvailable, Signatur = NoDataAvailable, Stufe = NoDataAvailable};
                result.Dossier = new VerzEinheitType
                {
                    Titel = NoDataAvailable, Signatur = NoDataAvailable, Entstehungszeitraum = NoDataAvailable, Stufe = NoDataAvailable,
                    VerzEinheitId = Convert.ToInt64(recordId)
                };
                result.Auftragsdaten = GetOrderData(recordId, result);
            }

            return result;
        }

        /// <summary>
        ///     Gets the dossier data, which includes the dossier of the ordered item and all its children units, including the
        ///     container of the children
        ///     and the units in the container.
        /// </summary>
        /// <param name="archiveRecord">The archive record.</param>
        /// <returns>VerzEinheitType.</returns>
        private VerzEinheitType GetDossierData(ArchiveRecord archiveRecord)
        {
            // No matter which level the ordered item was, we always need to deliver the whole dossier
            var dossierLevelIndex =
                archiveRecord.Display.ArchiveplanContext.FindIndex(i =>
                    i.Level.Equals(DossierLevelIdentifier, StringComparison.InvariantCultureIgnoreCase));
            if (dossierLevelIndex < 0)
            {
                throw new InvalidOperationException(
                    "We could not find a dossier. Please check your data if the ordered item is either a dossier or a document.");
            }

            var dossierId = archiveRecord.Display.ArchiveplanContext[dossierLevelIndex].ArchiveRecordId;
            var dossier = dataProvider.LoadOrderDetailData(Convert.ToInt32(dossierId));
            var dossierData = GetArchiveRecordDetailData(dossier);
            return dossierData;
        }

        private VerzEinheitType GetArchiveRecordDetailData(OrderDetailData verzEinheit)
        {
            var sipDate = sipDateBuilder.ConvertToValidSipDateString(
                verzEinheit.BeginStandardDate,
                verzEinheit.BeginApproxIndicator,
                verzEinheit.EndStandardDate,
                verzEinheit.EndApproxIndicator,
                verzEinheit.DateOperatorId.HasValue ? (ScopeArchivDateOperator) verzEinheit.DateOperatorId : (ScopeArchivDateOperator?) null,
                NoDataAvailable);

            Log.Debug("Processing detail data for archive record with id {archiveRecordId}", verzEinheit.Id);
            var retVal = new VerzEinheitType
            {
                VerzEinheitId = Convert.ToInt64(verzEinheit.Id),
                Titel = !string.IsNullOrEmpty(verzEinheit.Title) ? verzEinheit.Title : NoDataAvailable,
                Signatur = !string.IsNullOrEmpty(verzEinheit.ReferenceCode) ? verzEinheit.ReferenceCode : NoDataAvailable,
                Entstehungszeitraum = sipDate,
                Stufe = !string.IsNullOrEmpty(verzEinheit.Level) ? verzEinheit.Level : NoDataAvailable,
                Aktenzeichen = !string.IsNullOrEmpty(verzEinheit.DossierCode) ? verzEinheit.DossierCode : null,
                FrueheresAktenzeichen = !string.IsNullOrEmpty(verzEinheit.FormerDossierCode) ? verzEinheit.FormerDossierCode : null,
                Darin = !string.IsNullOrEmpty(verzEinheit.WithinRemark) ? verzEinheit.WithinRemark : null,
                Zusatzkomponente = !string.IsNullOrEmpty(verzEinheit.Zusatzkomponente) ? verzEinheit.Zusatzkomponente : null,
                Form = string.IsNullOrEmpty(verzEinheit.Form) ? null : verzEinheit.Form
            };

            // Do we have child records? If yes, add all the the collection
            var children = dataProvider.GetChildrenRecordOrderDetailDataForArchiveRecord(Convert.ToInt64(verzEinheit.Id));
            if (children.Any())
            {
                retVal.UntergeordneteVerzEinheiten = children.Select(GetArchiveRecordDetailData).ToList();
            }

            // Add all containers to item
            retVal.Behaeltnisse = GetBehaltnisse(verzEinheit);

            return retVal;
        }

        private List<BehaeltnisType> GetBehaltnisse(OrderDetailData verzEinheit)
        {
            List<BehaeltnisType> retVal = null;
            Log.Debug("Fetching containers for archive record with id {archiveRecordId}", verzEinheit.Id);
            var containers = dataProvider.LoadContainers(Convert.ToInt64(verzEinheit.Id));
            if (containers.StorageContainer.Rows.Count > 0)
            {
                retVal = new List<BehaeltnisType>();
                foreach (var container in containers.StorageContainer)
                {
                    retVal.Add(new BehaeltnisType
                    {
                        BehaeltnisCode = container.BHLTN_CD,
                        BehaeltnisTyp = container.BHLTN_TYP_NM,
                        InformationsTraeger = container.BHLTN_INFO_TRGR_NM,
                        Standort = container.BHLTN_DEF_STAND_ORT_CD,
                        EnthalteneVerzEinheiten =
                            verzEinheit.Level == DossierLevelIdentifier ? GetArchiveRecordsToContainer(container.BHLTN_ID) : null
                    });
                }
            }

            return retVal;
        }

        private List<VerzEinheitKurzType> GetArchiveRecordsToContainer(long containerId)
        {
            // Do we have the container contents in the cache?
            if (containerContentCache.ContainsKey(containerId))
            {
                return containerContentCache[containerId];
            }

            Log.Debug("Getting archive records in container with id {containerId}", containerId);
            var retVal = new List<VerzEinheitKurzType>();
            var archiveRecords = dataProvider.GetArchiveRecordOrderDetailDataForContainer(containerId);
            retVal.AddRange(archiveRecords.Select(r => new VerzEinheitKurzType
            {
                Titel = !string.IsNullOrEmpty(r.Title) ? r.Title : NoDataAvailable,
                Signatur = !string.IsNullOrEmpty(r.ReferenceCode) ? r.ReferenceCode : NoDataAvailable,
                Entstehungszeitraum = !string.IsNullOrEmpty(r.CreationPeriod) ? r.CreationPeriod : NoDataAvailable,
                Aktenzeichen = !string.IsNullOrEmpty(r.DossierCode) ? r.DossierCode : null
            }));

            // Add to the cache
            containerContentCache.GetOrAdd(containerId, retVal);

            return retVal;
        }

        /// <summary>
        ///     Gets the order data.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="digitizationOrderData">The digitization order data.</param>
        /// <returns>AuftragsdatenType.</returns>
        private AuftragsdatenType GetOrderData(string recordId, DigitalisierungsAuftrag digitizationOrderData)
        {
            // Only these values can be filled by the order builder
            var retVal = new AuftragsdatenType
            {
                BestelleinheitId = recordId,
                Benutzungskopie = IsUsageCopy(digitizationOrderData)
            };

            return retVal;
        }


        /// <summary>
        ///     Determines whether this order is a usage copy or not based on the data.
        /// </summary>
        /// <param name="digitizationOrderData">The digitization order data.</param>
        /// <returns><c>true</c> if it is a usage copy otherwise, <c>false</c>.</returns>
        public static bool IsUsageCopy(DigitalisierungsAuftrag digitizationOrderData)
        {
            // The rules are according to the specification
            /* Dieser Wert wird durch das System ermittelt. Dabei gelten die folgenden Regeln:
                –	AblieferungsType: Alle Felder müssen geliefert werden. Enthält eines der Felder den Dummy Wert «keine Angabe», 
                    handelt es sich um eine Benutzungskopie.
                –	OrdnungsSystemType: Sämtliche Felder müssen geliefert werden. Enthält eines der Felder den Dummy Wert «keine Angabe», 
                    handelt es sich um eine Benutzungskopie.
                –	VerzEinheitType: Es muss ein Wert für die Felder Titel und Entstehungszeitraum geliefert werden. Enthält eines der Felder den 
                    Dummy Wert «keine Angabe», handelt es sich um eine Benutzungskopie. 
                    –	Diese beiden Felder müssen auch für jeweils die untergeordneten Verzeichnungseinheiten vorhanden sein.  */

            // Instead of checking each individual field, we serialize the object and then try to search by regex
            // this reduces the complexity due to recursive fields we have

            var retVal = digitizationOrderData.Ablieferung.Ablieferungsnummer.Equals(NoDataAvailable);
            retVal = retVal || digitizationOrderData.Ablieferung.AblieferndeStelle.Equals(NoDataAvailable);
            retVal = retVal || digitizationOrderData.Ablieferung.AktenbildnerName.Equals(NoDataAvailable);

            retVal = retVal || CheckOrdnungsSystemForNoDataAvailable(digitizationOrderData.OrdnungsSystem);

            retVal = retVal || digitizationOrderData.Dossier.Titel.Equals(NoDataAvailable);
            retVal = retVal || digitizationOrderData.Dossier.Entstehungszeitraum.Equals(NoDataAvailable);
            retVal = retVal || CheckUntergeordneteVerzEinheitenNoDataAvailable(digitizationOrderData.Dossier.UntergeordneteVerzEinheiten);

            return retVal;
        }

        private static bool CheckUntergeordneteVerzEinheitenNoDataAvailable(List<VerzEinheitType> verzEinheiten)
        {
            if (verzEinheiten == null)
            {
                return false;
            }

            var retVal = verzEinheiten.Any(v => v.Titel.Equals(NoDataAvailable) || v.Entstehungszeitraum.Equals(NoDataAvailable));
            foreach (var verzEinheit in verzEinheiten)
            {
                retVal = retVal || CheckUntergeordneteVerzEinheitenNoDataAvailable(verzEinheit.UntergeordneteVerzEinheiten);
            }

            return retVal;
        }

        private static bool CheckOrdnungsSystemForNoDataAvailable(OrdnungsSystemType ordnungssystem)
        {
            var retVal = ordnungssystem.Name.Equals(NoDataAvailable);
            retVal = retVal || ordnungssystem.Signatur.Equals(NoDataAvailable);
            retVal = retVal || ordnungssystem.Stufe.Equals(NoDataAvailable);
            if (ordnungssystem.UntergeordnetesOrdnungsSystem != null)
            {
                retVal = retVal || CheckOrdnungsSystemForNoDataAvailable(ordnungssystem.UntergeordnetesOrdnungsSystem);
            }

            return retVal;
        }

        /// <summary>
        ///     Gets the ordering position data.
        ///     From the ordered position we have to go up until we find "Bestand" or "Teilbestand". This record, is the first
        ///     element.
        ///     From this found position we need to go down, until we find a "Dossier" which ends the search.
        ///     The "Dossier" is not part of the data.
        /// </summary>
        /// <param name="archiveRecord">The archive record.</param>
        /// <returns>
        ///     OrdnungsSystemType or throws an exception if no Bestand, or Teilbestand could be found higher up in the
        ///     hierarchy.
        /// </returns>
        private OrdnungsSystemType GetOrderingPositionData(ArchiveRecord archiveRecord)
        {
            // Do we find a subfonds?
            var startIndex = archiveRecord.Display.ArchiveplanContext.FindLastIndex(i =>
                i.Level.Equals(SubFondsLevelIdentifier, StringComparison.InvariantCultureIgnoreCase));
            if (startIndex < 0)
                // Do we find a fonds then?
            {
                startIndex = archiveRecord.Display.ArchiveplanContext.FindLastIndex(i =>
                    i.Level.Equals(FondsLevelIdentifier, StringComparison.InvariantCultureIgnoreCase));
            }

            if (startIndex < 0)
            {
                throw new InvalidOperationException(
                    "We could not find a fonds or subfonds. Please check your data if the ordered item is located below either a fonds or subfonds.");
            }

            // Now lets find the the first dossier which marks the end
            var endIndex = archiveRecord.Display.ArchiveplanContext.FindIndex(i =>
                i.Level.Equals(DossierLevelIdentifier, StringComparison.InvariantCultureIgnoreCase));
            if (endIndex < 0)
            {
                throw new InvalidOperationException(
                    "We could not find a dossier. Please check your data if the ordered item is either a dossier or a document.");
            }

            // Now we have the start and the end, now we just have to gather the data.
            var retVal = new OrdnungsSystemType();
            var current = new OrdnungsSystemType();
            for (var i = startIndex; i < endIndex; i++)
            {
                if (i == startIndex)
                {
                    current = retVal;
                }
                else
                {
                    current.UntergeordnetesOrdnungsSystem = new OrdnungsSystemType();
                    current = current.UntergeordnetesOrdnungsSystem;
                }

                current.Name = !string.IsNullOrEmpty(archiveRecord.Display.ArchiveplanContext[i].Title)
                    ? archiveRecord.Display.ArchiveplanContext[i].Title
                    : NoDataAvailable;
                current.Signatur = !string.IsNullOrEmpty(archiveRecord.Display.ArchiveplanContext[i].RefCode)
                    ? archiveRecord.Display.ArchiveplanContext[i].RefCode
                    : NoDataAvailable;
                current.Stufe = !string.IsNullOrEmpty(archiveRecord.Display.ArchiveplanContext[i].Level)
                    ? archiveRecord.Display.ArchiveplanContext[i].Level
                    : NoDataAvailable;

                // Auf Stufe Serie muss die Serie-Nummer geliefert werden. Dies ist die Nummer nach dem letzten #
                if (current.Stufe == SerieLevelIdentifier)
                {
                    var pattern = @"^.*#(?<number>[^#]+)$";
                    var r = Regex.Match(current.Signatur, pattern);
                    if (!r.Success)
                    {
                        throw new ArgumentOutOfRangeException(nameof(current.Signatur), current.Signatur,
                            "Der Signatur der Stufe Serie fehlt die Serie-Nummer. Die Signatur ist nicht gültig.");
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        ///     Gets the accession data.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="archiveRecord">The archive record.</param>
        /// <returns>AblieferungType.</returns>
        private AblieferungType GetAccessionData(string recordId, ArchiveRecord archiveRecord)
        {
            var retVal = new AblieferungType();
            // Get the accession
            var accession = dataProvider.GetLinkedAccessionToArchiveRecord(Convert.ToInt64(recordId));
            if (accession != null)
            {
                retVal.AblieferndeStelle = accession.ABLFR_PRTNR_KURZ_NM;
                retVal.Ablieferungsnummer = $"{accession.ABLFR_JAHR}/{Convert.ToInt32(accession.ABLFR_NR)}";
            }
            else
            {
                retVal.AblieferndeStelle = NoDataAvailable;
                retVal.Ablieferungsnummer = NoDataAvailable;
            }

            // Set the accession builder name
            retVal.AktenbildnerName = GetAccessionBuilderName(archiveRecord.Display.ArchiveplanContext);

            return retVal;
        }

        /// <summary>
        ///     Gets the name of the accession builder.
        /// </summary>
        /// <param name="archivePlan">The archive plan.</param>
        /// <returns>System.String.</returns>
        private string GetAccessionBuilderName(List<ArchiveplanContextItem> archivePlan)
        {
            // We have to find the data element with valid data that can be found either directly on the record
            // or one of its parent
            for (var i = archivePlan.Count - 1; i >= 0; i--)
            {
                var deTable = dataProvider.GetDetailDataForElement(Convert.ToInt64(archivePlan[i].ArchiveRecordId),
                    (int) ScopeArchivDatenElementId.AktenbildnerProvenienzLink);
                if (deTable.Rows.Count > 0)
                {
                    // Get the first linked element (should always be one)
                    var deRow = deTable.AsEnumerable().First();
                    var partnerId = deRow.VRKNP_GSFT_OBJ_ID;
                    var partnerIdName = dataProvider.GetBusinessObjectIdName(Convert.ToInt64(partnerId));
                    return partnerIdName;
                }
            }

            return NoDataAvailable;
        }
    }
}