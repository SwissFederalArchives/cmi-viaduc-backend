using CMI.Access.Harvest.ActaPro.Mapping;
using CMI.Contract.Common;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CMI.Access.Harvest.ActaPro;

public class ActaProDigitizationOrderBuilder : IDigitizationOrderBuilder
{
    public const string NoDataAvailable = "keine Angabe";
    private const string dossierLevelIdentifier = "Dossier";
    private const string subFondsLevelIdentifier = "Teilbestand";
    private const string fondsLevelIdentifier = "Bestand";
    private const string serieLevelIdentifier = "Serie";
    private readonly ConcurrentDictionary<string, List<VerzEinheitKurzType>> containerContentCache;
    private readonly IAISDataProvider dataProvider;
    private readonly IArchiveRecordBuilder recordBuilder;

    public ActaProDigitizationOrderBuilder(IAISDataProvider dataProvider, IArchiveRecordBuilder recordBuilder)
    {
        this.dataProvider = dataProvider;
        this.recordBuilder = recordBuilder;
        containerContentCache = new ConcurrentDictionary<string, List<VerzEinheitKurzType>>();
    }

    /// <summary>
    ///     Builds the digitization order from a given record id.
    ///     The record id should be from a unit of description which is either a document or dossier.
    /// </summary>
    /// <param name="recordId">The record identifier.</param>
    /// <returns>DigitalisierungsAuftrag.</returns>
    public async Task<DigitalisierungsAuftrag> Build(string recordId)
    {
        var result = new DigitalisierungsAuftrag();

        // clear cache 
        containerContentCache.Clear();

        // Get lots of metadata we need for processing
        var archiveRecord = await recordBuilder.Build(recordId);

        if (archiveRecord != null)
        {
            // Accession Data
            var t1 = Task.Factory.StartNew(async () => { result.Ablieferung = await GetAccessionData(recordId, archiveRecord.Display.ArchiveplanContext); }).Result;

            var t2 = Task.Factory.StartNew(() =>
            {
                // Ordering position Data
                result.OrdnungsSystem = GetOrderingPositionData(archiveRecord);
            });

            var t3 = Task.Factory.StartNew(async () =>
            {
                // Archive record data
                result.Dossier = await GetDossierData(archiveRecord);
            }).Result;

            Task.WaitAll(t1, t2, t3);

            // The basic order data
            result.Auftragsdaten = GetOrderData(recordId, result);
        }
        else
        {
            result.Ablieferung = new AblieferungType
            { AblieferndeStelle = NoDataAvailable, Ablieferungsnummer = NoDataAvailable, AktenbildnerName = NoDataAvailable };
            result.OrdnungsSystem = new OrdnungsSystemType { Name = NoDataAvailable, Signatur = NoDataAvailable, Stufe = NoDataAvailable };
            result.Dossier = new VerzEinheitType
            {
                Titel = NoDataAvailable, Signatur = NoDataAvailable, Entstehungszeitraum = NoDataAvailable, Stufe = NoDataAvailable,
                VerzEinheitId = recordId
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
    private async Task<VerzEinheitType> GetDossierData(ArchiveRecord archiveRecord)
    {
        // No matter which level the ordered item was, we always need to deliver the whole dossier
        var dossierLevelIndex =
            archiveRecord.Display.ArchiveplanContext.FindIndex(i =>
                i.Level.Equals(dossierLevelIdentifier, StringComparison.InvariantCultureIgnoreCase));
        if (dossierLevelIndex < 0)
        {
            throw new InvalidOperationException(
                "We could not find a dossier. Please check your data if the ordered item is either a dossier or a document.");
        }

        var dossierId = archiveRecord.Display.ArchiveplanContext[dossierLevelIndex].ArchiveRecordId;
        var dossier = await recordBuilder.Build(dossierId);
        var dossierData = await GetArchiveRecordDetailData(dossier);
        return dossierData;
    }

    private async Task<VerzEinheitType> GetArchiveRecordDetailData(ArchiveRecord verzEinheit)
    {
        // If invalid an error is raised
        VerifyFormattedDate(verzEinheit.Metadata.DetailData, out string sipDate);

        Log.Debug("Processing detail data for archive record with id {archiveRecordId}", verzEinheit.ArchiveRecordId);
        var titel = GetDataElementValue(verzEinheit.Metadata.DetailData, "TITEL", "string").ToString();
        var signatur = GetDataElementValue(verzEinheit.Metadata.DetailData, "SIGNATUR", "string").ToString();
        var stufe = GetDataElementValue(verzEinheit.Metadata.DetailData, "STUFE", "string").ToString();
        var aktenzeichen = GetDataElementValue(verzEinheit.Metadata.DetailData, "AKTENZEICHEN", "string").ToString();
        var frueheresAktenzeichen = GetDataElementValue(verzEinheit.Metadata.DetailData, "FRÜHERES_AKTENZEICHEN", "string").ToString();
        var darin = GetDataElementValue(verzEinheit.Metadata.DetailData, "DARIN", "string").ToString();
        var zusatzKomponente = GetDataElementValue(verzEinheit.Metadata.DetailData, "ZUSATZKOMPONENTE_ZAC1", "string").ToString();
        var form = GetDataElementValue(verzEinheit.Metadata.DetailData, "FORM", "string").ToString();
        var retVal = new VerzEinheitType
        {
            VerzEinheitId = verzEinheit.ArchiveRecordId,
            Titel = !string.IsNullOrEmpty(titel) ? titel : NoDataAvailable,
            Signatur = !string.IsNullOrEmpty(signatur) ? signatur : NoDataAvailable,
            Entstehungszeitraum = sipDate,
            Stufe = !string.IsNullOrEmpty(stufe) ? stufe : NoDataAvailable,
            Aktenzeichen = !string.IsNullOrEmpty(aktenzeichen) ? aktenzeichen : null,
            FrueheresAktenzeichen = !string.IsNullOrEmpty(frueheresAktenzeichen) ? frueheresAktenzeichen : null,
            Darin = !string.IsNullOrEmpty(darin) ? darin : null,
            Zusatzkomponente = !string.IsNullOrEmpty(zusatzKomponente) ? zusatzKomponente : null,
            Form = !string.IsNullOrEmpty(form) ? form : null
        };

        // Do we have child records? If yes, add them to the collection
        var children = await dataProvider.GetDocumentChildrenAsync(verzEinheit.ArchiveRecordId);
        if (children.Any())
        {
            retVal.UntergeordneteVerzEinheiten = new List<VerzEinheitType>();
        }
        foreach (var child in children)
        {
            var doc = await recordBuilder.Build(child);
            var childRecord = await GetArchiveRecordDetailData(doc);
            retVal.UntergeordneteVerzEinheiten.Add(childRecord);
        }
        // Add all containers to item
        retVal.Behaeltnisse = await GetBehaltnisse(verzEinheit);

        return retVal;
    }

    /// <summary>
    /// Verifies (simplified) if the formatted date from ActaPro is "valid"
    /// </summary>
    /// <param name="dataElements"></param>
    /// <param name="sipDate"></param>
    private void VerifyFormattedDate(List<DataElement> dataElements, out string sipDate)
    {
        const string argumentExceptionNoData = "Es ist keine Zeitraumangabe vorhanden.";
        const string argumentExceptionMessage = "Es sind nur Datumsangaben im Format JJJJ oder TT.MM.JJJJ erlaubt.";
        const string argumentOpenDatenRangesExceptionMessage = "Es sind keine 'offenen' Zeiträume wie < 1900 oder > 2000 erlaubt.";

        var simpleDateRange = GetDataElementValue(dataElements, "ENTSTEHUNGSZEITRAUM", "dateRange") as SimplifiedDateRange;
        if (simpleDateRange == null)
        {
            Log.Information("Konnte kein simpleDateRange erzeugen. Daten zur Analyse: {dataElements}", JsonConvert.SerializeObject(dataElements));
            throw new ArgumentOutOfRangeException("entstehungszeitraum", argumentExceptionNoData);
        }


        // "offene" Zeiträume sollen als Fehler behandelt werden. 
        // Auch ist sd und na nicht erlaubt
        switch (simpleDateRange.DateOperator)
        {
            case DateRangeDateOperator.after:
            case DateRangeDateOperator.startingWith:
                Log.Information("Offener Zeitraum gefunden (after). Daten zur Analyse: {dataElements}", JsonConvert.SerializeObject(dataElements));
                throw new ArgumentOutOfRangeException(nameof(simpleDateRange.FromDate), argumentOpenDatenRangesExceptionMessage);
            case DateRangeDateOperator.before:
            case DateRangeDateOperator.to:
                Log.Information("Offener Zeitraum gefunden (before). Daten zur Analyse: {dataElements}", JsonConvert.SerializeObject(dataElements));
                throw new ArgumentOutOfRangeException(nameof(simpleDateRange.ToDate), argumentOpenDatenRangesExceptionMessage);
            case DateRangeDateOperator.na:
            case DateRangeDateOperator.sd:
                Log.Information("Zeitram N/A oder sine dato. Daten zur Analyse: {dataElements}", JsonConvert.SerializeObject(dataElements));
                throw new ArgumentOutOfRangeException(nameof(simpleDateRange.DateOperator), argumentExceptionMessage);
        }
        
        sipDate = simpleDateRange.FormattedDate;

    }

    private async Task<List<BehaeltnisType>> GetBehaltnisse(ArchiveRecord verzEinheit)
    {
        List<BehaeltnisType> retVal = null;
        Log.Debug("Fetching containers for archive record with id {archiveRecordId}", verzEinheit.ArchiveRecordId);
        var containers = verzEinheit.Metadata.Containers.Container;
        if (containers.Count > 0)
        {
            retVal = new List<BehaeltnisType>();
            foreach (var container in containers.Where(c => !string.IsNullOrEmpty(c.ContainerCode)))
            {
                retVal.Add(new BehaeltnisType
                {
                    BehaeltnisCode = container.ContainerCode,
                    BehaeltnisTyp = container.ContainerType,
                    InformationsTraeger = container.ContainerCarrierMaterial,
                    Standort = container.ContainerLocation,
                    EnthalteneVerzEinheiten =
                        verzEinheit.Metadata.DetailData.First(d => d.ElementName.Equals("STUFE")).ElementValue[0].TextValues[0].Value 
                        == dossierLevelIdentifier ? await GetArchiveRecordsToContainer(container.ContainerId) : null
                });
            }
        }

        return retVal;
    }

    private async Task<List<VerzEinheitKurzType>> GetArchiveRecordsToContainer(string containerId)
    {

        // Do we have the container contents in the cache?
        if (containerContentCache.TryGetValue(containerId, out var container))
        {
            return container;
        }

        Log.Debug("Getting archive records in container with id {containerId}", containerId);
        var retVal = new List<VerzEinheitKurzType>();
        var archiveRecords = await dataProvider.GetArchiveRecordOrderDetailDataForContainer(containerId);
        retVal.AddRange(archiveRecords.Select(r => new VerzEinheitKurzType
        {
            Titel = !string.IsNullOrEmpty(r.Title) ? r.Title : NoDataAvailable,
            Signatur = !string.IsNullOrEmpty(r.ReferenceCode) ? r.ReferenceCode : NoDataAvailable,
            Entstehungszeitraum = !string.IsNullOrEmpty(r.CreationPeriod) ? r.CreationPeriod : NoDataAvailable,
            Aktenzeichen = !string.IsNullOrEmpty(r.Aktenzeichen) ? r.Aktenzeichen : null
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
            i.Level.Equals(subFondsLevelIdentifier, StringComparison.InvariantCultureIgnoreCase));
        if (startIndex < 0)
        // Do we find a fonds then?
        {
            startIndex = archiveRecord.Display.ArchiveplanContext.FindLastIndex(i =>
                i.Level.Equals(fondsLevelIdentifier, StringComparison.InvariantCultureIgnoreCase));
        }

        if (startIndex < 0)
        {
            throw new InvalidOperationException(
                "We could not find a fonds or subfonds. Please check your data if the ordered item is located below either a fonds or subfonds.");
        }

        // Now lets find the first dossier which marks the end
        var endIndex = archiveRecord.Display.ArchiveplanContext.FindIndex(i =>
            i.Level.Equals(dossierLevelIdentifier, StringComparison.InvariantCultureIgnoreCase));
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
            if (current.Stufe == serieLevelIdentifier)
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
    /// <param name="displayArchiveplanContext"></param>
    /// <returns>AblieferungType.</returns>
    private async Task<AblieferungType> GetAccessionData(string recordId, List<ArchiveplanContextItem> archivplanContext)
    {
        var retVal = new AblieferungType();

        var document = await dataProvider.GetAisDataRecordAsync(recordId);
        if (document == null)
        {
            return retVal;
        }

        var ablieferungGroupFields = document.Block.Fields
            .FirstOrDefault(f => f.Type.Equals("Ablieferung_Gp", StringComparison.InvariantCultureIgnoreCase))?.Fields;


        if (ablieferungGroupFields != null)
        {
            var ablId = MappingFunctions.GetFieldValue(ablieferungGroupFields, "Ablieferung_Key");
            var ablDocument = await dataProvider.GetAisDataRecordAsync(ablId);

            // Initialize some variables
            var ablStelle = string.Empty;
            var aktenStelle = string.Empty;
            var ablNum = MappingFunctions.GetFieldValue(ablieferungGroupFields, "Ablieferung_AblNum");

            // Get the Abliefernde Stelle
            var ablieferndeStelleGroupFields = ablDocument.Block.Fields
                .FirstOrDefault(f => f.Type.Equals("Abl_AblieferndeStelle_Gp", StringComparison.InvariantCultureIgnoreCase))?.Fields;
            if (ablieferndeStelleGroupFields != null)
            {
                ablStelle = MappingFunctions.GetFieldValue(ablieferndeStelleGroupFields, "Abl_AblieferndeStelle");
            }

            // Get the Aktenbildende Stelle
            aktenStelle = await GetAccessionBuilderName(archivplanContext);

            retVal.AblieferndeStelle = !string.IsNullOrEmpty(ablStelle) ? ablStelle : NoDataAvailable;
            retVal.Ablieferungsnummer = !string.IsNullOrEmpty(ablNum) ? ablNum : NoDataAvailable;
            retVal.AktenbildnerName = !string.IsNullOrEmpty(aktenStelle) ? aktenStelle : NoDataAvailable;
        }
        else
        {
            retVal.AblieferndeStelle = NoDataAvailable;
            retVal.Ablieferungsnummer = NoDataAvailable;
            retVal.AktenbildnerName = NoDataAvailable;
        }

        return retVal;
    }

    /// <summary>
    ///     Gets the name of the accession builder.
    /// </summary>
    /// <param name="archivePlan">The archive plan.</param>
    /// <returns>System.String.</returns>
    private async Task<string> GetAccessionBuilderName(List<ArchiveplanContextItem> archivePlan)
    {
        // We have to find the data element with valid data that can be found either directly on the record
        // or one of its parent
        for (var i = archivePlan.Count - 1; i >= 0; i--)
        {
            var document = await dataProvider.GetAisDataRecordAsync(archivePlan[i].ArchiveRecordId);
            if (document != null)
            {
                var aktenbildendeStelleGroupFields = document.Block.Fields
                    .FirstOrDefault(f => f.Type.Equals("Aktenbildner_Gp", StringComparison.InvariantCultureIgnoreCase))?.Fields;
                if (aktenbildendeStelleGroupFields != null)
                {
                    var aktenStelle = MappingFunctions.GetFieldValue(aktenbildendeStelleGroupFields, "Aktenbildner_Bez");
                    return aktenStelle;
                }
            }
        }

        return NoDataAvailable;
    }

    private object GetDataElementValue(List<DataElement> detailData, string elementName, string fieldType)
    {
        try
        {
            var dataElement = detailData.FirstOrDefault(d => elementName.Equals(d.ElementName, StringComparison.InvariantCultureIgnoreCase));
            if (dataElement?.ElementValue == null)
            {
                return string.Empty;
            }

            switch (fieldType.ToLower())
            {
                case "string":
                    string stringVal = null;
                    foreach (var elementValue in dataElement.ElementValue.OrderBy(t => t.Sequence))
                    {
                        stringVal += elementValue.TextValues.FirstOrDefault(t => t.IsDefaultLang)?.Value;
                    }

                    return stringVal ?? "";
                case "daterange":
                    var dateRangeElementValue = dataElement.ElementValue[0];
                    return new SimplifiedDateRange
                    {
                        DateOperator = dateRangeElementValue.DateRange.DateOperator,
                        FromDate = dateRangeElementValue.DateRange.FromDate,
                        ToDate = dateRangeElementValue.DateRange.ToDate,
                        FormattedDate =  FormatDate(dateRangeElementValue)
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            throw;
        }
    }

    private string FormatDate(DataElementElementValue dateRangeElementValue)
    {
        if (dateRangeElementValue.DateRange.FromDate == dateRangeElementValue.DateRange.ToDate)
        {
            return $"{dateRangeElementValue.DateRange.FromDate:dd.MM.yyyy}";
        }

        return $"{dateRangeElementValue.DateRange.FromDate:dd.MM.yyyy}-{dateRangeElementValue.DateRange.ToDate:dd.MM.yyyy}";
    }

    private class SimplifiedDateRange
    {
        public DateRangeDateOperator DateOperator { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FormattedDate { get; set; }
    }
}