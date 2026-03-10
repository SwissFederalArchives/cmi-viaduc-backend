using CMI.Access.Harvest.ActaPro.Security;
using CMI.Contract.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CMI.Access.Harvest.ActaPro.Mapping;

public class ArchiveRecordMapper
{
    public ArchiveRecordMetadata LoadMetadata(Document document, DocumentAncestorsDTO ancestors, long childCount)
    {
        var retVal = new ArchiveRecordMetadata();

        var data = new List<DataElement>();
        if (document != null && document.AdditionalProperties.ContainsKey("type") &&
            document.AdditionalProperties.TryGetValue("type", out var levelObject))
        {
            // Lade die verschiedenen Datenelemente
            LoadDataElements(document, data, levelObject.ToString());

            // Ablieferungsjahr
            var value = MappingFunctions.GetFieldValue(document, "Abl_Jahr");
            if (value != null)
            {
                retVal.AccessionDate = int.Parse(value);
            }

            // Primary data Link
            retVal.PrimaryDataLink = MappingFunctions.GetFieldValue(document, "IdentDigiMagazin");

            retVal.Usage = MapMetadataUsage(document);

            // Node Info (as much as we can do here)
            retVal.NodeInfo.ParentArchiveRecordId = GetParentArchiveRecord(document.Block.Fields);
            retVal.NodeInfo.Sequence = GetTreeSequence(document.Block.Fields);
            // Path is a semicolon separated string
            var path = ancestors.Document["path"].ToString();
            var pathItems = path.Split(';');
            retVal.NodeInfo.Path = string.Join("", pathItems.Select(s => s.Trim()));
            retVal.NodeInfo.Level = pathItems.Length;
            retVal.NodeInfo.ChildCount = childCount;
            retVal.NodeInfo.IsLeaf = childCount == 0;

        }

        retVal.DetailData = data;
        return retVal;
    }

    private string GetParentArchiveRecord(ICollection<DocumentField> fields)
    {
        var groupFields = fields.FirstOrDefault(f => f.Type.Equals("Ref_Gp") &&
                                                     f.Fields.Any(t => t.Value.Equals("P")))?.Fields;
        if (groupFields != null)
        {
            return groupFields.FirstOrDefault(f => f.Type.Equals("Ref_DocKey", StringComparison.InvariantCultureIgnoreCase))?.Value;
        }

        return null;
    }

    private int GetTreeSequence(ICollection<DocumentField> fields)
    {
        var groupFields = fields.FirstOrDefault(f => f.Type.Equals("Ref_Gp") &&
                                                     f.Fields.Any(t => t.Value.Equals("P")))?.Fields;
        if (groupFields != null)
        {
            int.TryParse(groupFields.FirstOrDefault(f => f.Type.Equals("Ref_DocOrder", StringComparison.InvariantCultureIgnoreCase))?.Value,
                out var seq);
            {
                return seq;
            }
        }

        return 0;
    }

    private void LoadDataElements(Document document, List<DataElement> data, string levelObject)
    {
        var dataElementStufe = MapStufe(levelObject);

        data.Add(dataElementStufe);
        data.Add(MapTitle(document.Block.Fields, levelObject));

        if (document.Block.Fields.Any(f => f.Type.Contains("_ZusatzInfo_")))
        {
            data.Add(MappingFunctions.MapGroupFieldLevelDependent(document.Block.Fields, levelObject, "ZusatzInfo", "BEMERKUNG_ZUR_VE"));
        }
        else if (document.Block.Fields.Any(f => f.Type.Contains("_ZusatzInfo")))
        {
            data.Add(MapZusatzInfo(document.Block.Fields, levelObject, "BEMERKUNG_ZUR_VE"));
        }

        if (document.Block.Fields.Any(f => f.Type.Contains("_Land_")))
        {
            data.Add(MappingFunctions.MapGroupFieldLevelDependent(document.Block.Fields, levelObject, "Land", "LAND"));
        }

        if (document.Block.Fields.Any(f => f.Type.Contains("_Darin_")))
        {
            data.Add(MappingFunctions.MapGroupFieldLevelDependent(document.Block.Fields, levelObject, "Darin", "DARIN"));
        }
        else if (document.Block.Fields.Any(f => f.Type.Contains("_Darin")))
        {
            data.Add(MapDarin(document.Block.Fields, levelObject, "DARIN"));
        }

        data.Add(MapSignatur(document.Block.Fields, levelObject, "SIGNATUR"));
        data.Add(MapSignatur(document.Block.Fields, levelObject, "SIGNATUR_ARCHIVPLAN"));
        data.Add(MapGeschichteDerUnterlagen(document.Block.Fields, levelObject));
        
        if (document.Block.Fields.Any(f => f.Type.StartsWith("Altsi")))
        {
            data.Add(MappingFunctions.MapGroupField(document.Block.Fields, "Altsi", "SIGNATURHISTORY"));
        }

        if (document.Block.Fields.Any(f => f.Type.StartsWith("Verw_Ve")))
        {
            data.Add(MappingFunctions.MapGroupField(document.Block.Fields, "Verw_Ve", "VERWANDTE_VE"));
        }

        if (document.Block.Fields.Any(f => f.Type.StartsWith("Ablieferung")))
        {
            data.Add(MappingFunctions.CreateVerknüpfungElement(document.Block.Fields, "Ablieferung_Gp", "Ablieferung_Bez", "Ablieferung_Key", "VE_ABLIEFERUNG_LINK", "Ablieferungen"));
        }

        if (document.Block.Fields.Any(f => f.Type.StartsWith("Bestaendeuebersicht")))
        {
            data.Add(MappingFunctions.CreateVerknüpfungElement(document.Block.Fields, "Bestaendeuebersicht_Gp", "Bestaendeuebersicht_Bez", "Bestaendeuebersicht_Key", "VE_ORDNUNGSKOMPONENTE_LINK", ""));
        }

        var zustaendigeStellen = ExtractZustaendigeStellen(document);
        if (zustaendigeStellen.Any())
        {
            data.Add(MappingFunctions.CreateVerknüpfungElement(zustaendigeStellen, "ZUSTÄNDIGE_STELLE_(LINK)", "Part"));
        }

        if (document.Block.Fields.Any(f => f.Type.StartsWith("Vorgaengerbehoerde")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetGroupBezValue(document.Block.Fields, "Vorgaengerbehoerde"), "VORGÄNGERBEHÖRDEN"));
        }

        if (document.Block.Fields.Any(f => f.Type.StartsWith("Nachfolgebehoerde")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetGroupBezValue(document.Block.Fields, "Nachfolgebehoerde"), "NACHFOLGERBEHÖRDEN"));
        }

        var mapDateTimeElement = MappingFunctions.MapDateTimeElement(document.Block.Fields, "Laufzeit", "ENTSTEHUNGSZEITRAUM");
        if (mapDateTimeElement != null)
        {
            data.Add(mapDateTimeElement);
        }

        // sind bei allen Stufen vorhanden, trotz "Vz" start
        if (document.Block.Fields.Any(f => f.Type.StartsWith("Vz_Umfang")))
        {
            data.Add(MappingFunctions.MapGroupField(document.Block.Fields, "Vz_Umfang", "Umfang"));
        }

        if (document.Block.Fields.Any(f => f.Type.StartsWith("Vz_Az")))
        {
            data.Add(MappingFunctions.MapGroupField(document.Block.Fields, "Vz_Az", "FRÜHERES_AKTENZEICHEN"));
        }

        if (document.Block.Fields.Any(f => f.Type.StartsWith("Form")))
        {
            data.Add(MappingFunctions.MapGroupField(document.Block.Fields, "Form", "FORM"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_Urheber")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Vz_Urheber"), "URHEBER"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_BeteilPersKoerpersch")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Vz_BeteilPersKoerpersch"),
                "BETEILIGTE_PERSONEN_KÖRPERSCHAFTEN"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_Verleger")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Vz_Verleger"), "VERLEGER"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_Abdeckung")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Vz_Abdeckung"), "ABDECKUNG"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_Thema")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Vz_Thema"), "THEMA"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_Format")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Vz_Format"), "FORMAT"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_ZugaenglichkeitBGA")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Vz_ZugaenglichkeitBGA"),
                "ZUGÄNGLICHKEIT_GEMÄSS_BGA"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("ScopeID")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "ScopeID"), "ScopeID"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("IdentDigiMagazin")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "IdentDigiMagazin"), "AIP_ID"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Laufzeit_Anm")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Laufzeit_Anm"), "ENTSTEHUNGSZEITRAUM__ANM"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_Zusatzkomponente")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Vz_Zusatzkomponente"), "ZUSATZKOMPONENTE_ZAC1"));
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_Aktenzeichen")))
        {
            data.Add(MappingFunctions.CreateTextElement(MappingFunctions.GetFieldValue(document, "Vz_Aktenzeichen"), "AKTENZEICHEN"));
        }

        if (document.Block.Fields.Any(f => f.Type.StartsWith("Ablieferung_Gp")))
        {
            data.Add(MappingFunctions.CreateTextElement(
                MappingFunctions.GetFieldValue(document.Block.Fields, "Ablieferung_Gp", "Ablieferung_Publikationsrechte"), "PUBLIKATIONSRECHTE"));
        }

        var mapBoolElement = MappingFunctions.CreateBoolElement(document.Block.Fields, "Ablieferung_Gp", "Ablieferung_Anhang3VBGA", "ANHANG_3");
        if (mapBoolElement != null)
        {
            data.Add(mapBoolElement);
        }

        var mapIntegerElement = MappingFunctions.CreateIntegerElement(document.Block.Fields, "Ablieferung_Gp", "Ablieferung_KatAutomatisierung", "KATEGORIE_DIA");
        if (mapIntegerElement != null)
        {
            data.Add(mapIntegerElement);
        }

        if (document.Block.Fields.Any(f => f.Type.Equals("Vz_DigiVersion_Gp")))
        {
            data.Add(MappingFunctions.CreateHyperlinkElement(document.Block.Fields, "Vz_DigiVersion_Gp", "Vz_DigiVersion_Bez", "Vz_DigiVersion_URL",
                "DIGITALE_VERSION"));
        }

    }

    private ArchiveRecordMetadataUsage MapMetadataUsage(Document document)
    {
        var schutzfristdauerElement = MappingFunctions.MapDateTimeElement(document.Block.Fields, "Vz_Schutzfristende", "Vz_Schutzfristdauer");
        var usage = new ArchiveRecordMetadataUsage
        {
            ProtectionCategory = MappingFunctions.GetFieldValue(document, "Vz_Schutzfristkategorie"),
            ProtectionDuration = document.Block.Fields.Any(f => f.Type.Equals("Vz_Schutzfristdauer", StringComparison.InvariantCultureIgnoreCase))
                ? int.Parse(MappingFunctions.GetFieldValue(document, "Vz_Schutzfristdauer"))
                : 0,
            // If time extracted is 00:00:00 then change it to 23:59:59
            ProtectionEndDate = schutzfristdauerElement?.ElementValue[0]?.DateRange?.ToDate.TimeOfDay == new TimeSpan(0) ? 
                schutzfristdauerElement?.ElementValue[0]?.DateRange?.ToDate.AddDays(1).AddSeconds(-1) : 
                schutzfristdauerElement?.ElementValue[0]?.DateRange?.ToDate,
            Permission = MappingFunctions.GetFieldValue(document, "Bewilligungstyp"),
            PhysicalUsability = MappingFunctions.GetFieldValue(document, "Benutzbarkeit"),
            Accessibility = MappingFunctions.GetFieldValue(document, "Zugaenglichkeit"),
            AlwaysVisibleOnline = Convert.ToBoolean(MappingFunctions.GetFieldValue(document, "MetadatenPubl")?.Equals("Ja", StringComparison.InvariantCultureIgnoreCase)),
        };

        usage.IsPhysicalyUsable = Convert.ToBoolean(usage.PhysicalUsability?.Equals("Uneingeschränkt", StringComparison.InvariantCultureIgnoreCase));

        return usage;
    }

    private DataElement MapStufe(string value)
    {
        var fieldValue = MapStufeText(value);
        return MappingFunctions.CreateTextElement(fieldValue, "STUFE");
    }

    private static string MapStufeText(string value)
    {
        string fieldValue;
        switch (value.ToLower())
        {
            // Dossier, Subdossier
            case "vz":
                fieldValue = ActaProClientValues.StufeDossier;

                break;
            case "vor":
                fieldValue = ActaProClientValues.StufeSubdossier;

                break;
            case "dokum":
                fieldValue = ActaProClientValues.StufeDokument;
                break;

            case "klas":
                fieldValue = ActaProClientValues.StufeSerie;
                break;

            case "best": //Bestand, Teilbestand
                fieldValue = ActaProClientValues.StufeBestand;
                break;
            case "tbest":
                fieldValue = ActaProClientValues.StufeTeilbestand;
                break;
            case "tekt": //Tektonik / Hauptabteilung
                fieldValue = ActaProClientValues.StufeHauptabteilung;
                break;
            case "arch": //Archiv
                fieldValue = ActaProClientValues.StufeArchiv;
                break;
            default:
                throw new ArgumentException($"MapStufe do not work. Level: {value}");
        }

        return fieldValue;
    }

    public DataElement MapTitle(ICollection<DocumentField> fields, string levelValue)
    {
        var fieldValue = string.Empty;

        switch (levelValue.ToLower())
        {
            case "vz":
            case "dokum":
            case "vor": // Dossier, Subdossier, Dokument
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Vz_Titel", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            case "klas": // Klassifkation / Serie

                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Kl_Name", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            case "tbest":
            case "best": //Bestand, Teilbestand
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Bst_Name", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            case "tekt": //Tektonik / Hauptabteilung
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Te_Name", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;
            case "arch": //Archiv
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Ar_Name", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;
        }

        return MappingFunctions.CreateTextElement(fieldValue, "TITEL");
    }

    private string MapTitle(IDictionary<string, object> fieldValues, string levelType)
    {
        string fieldValue;
        switch (levelType.ToLower())
        {
            // Dossier, Subdossier, Dokument
            case "vz":
            case "dokum":
            case "vor":
                fieldValue = fieldValues.ContainsKey("Vz_Titel") ? fieldValues["Vz_Titel"].ToString() : "";
                break;

            // Klassifkation / Serie
            case "klas":
                fieldValue = fieldValues.ContainsKey("Kl_Name") ? fieldValues["Kl_Name"].ToString() : "";
                break;

            //Bestand, Teilbestand
            case "tbest":
            case "best":
                fieldValue = fieldValues.ContainsKey("Bst_Name") ? fieldValues["Bst_Name"].ToString() : "";
                break;

            //Tektonik / Hauptabteilung
            case "tekt":
                fieldValue = fieldValues.ContainsKey("Te_Name") ? fieldValues["Te_Name"].ToString() : "";
                break;
            //Archiv
            case "arch":
                fieldValue = fieldValues.ContainsKey("Ar_Name") ? fieldValues["Ar_Name"].ToString() : "";
                break;
            default:
                throw new ArgumentException($"Level is not supported {levelType}");
        }

        return fieldValue;
    }

    private DataElement MapDarin(ICollection<DocumentField> fields, string levelValue, string elementName)
    {
        string fieldValue;

        switch (levelValue.ToLower())
        {
            // Dossier, Subdossier, Dokument
            case "vz":
            case "dokum":
            case "vor":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Vz_Darin", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            // Klassifkation / Serie
            case "klas":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Kl_Darin", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            //Bestand, Teilbestand
            case "tbest":
            case "best":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Bst_Darin", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            //Tektonik / Hauptabteilung
            case "tekt":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Te_Darin", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;
            //Archiv
            case "arch":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Ar_Darin", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;
            default:
                throw new ArgumentException($"Level is not supported {levelValue}");
        }

        return MappingFunctions.CreateTextElement(fieldValue, elementName);
    }

    private DataElement MapZusatzInfo(ICollection<DocumentField> fields, string levelValue, string elementName)
    {
        string fieldValue;

        switch (levelValue.ToLower())
        {
            // Dossier, Subdossier, Dokument
            case "vz":
            case "dokum":
            case "vor":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Vz_ZusatzInfo", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            // Klassifkation / Serie
            case "klas":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Kl_ZusatzInfo", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            //Bestand, Teilbestand
            case "tbest":
            case "best":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Bst_ZusatzInfo", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            //Tektonik / Hauptabteilung
            case "tekt":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Te_ZusatzInfo", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;
            //Archiv
            case "arch":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Ar_ZusatzInfo", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;
            default:
                throw new ArgumentException($"Level is not supported {levelValue}");
        }

        return MappingFunctions.CreateTextElement(fieldValue, elementName);
    }

    private DataElement MapSignatur(ICollection<DocumentField> fields, string levelValue, string elementName)
    {
        string fieldValue;

        switch (levelValue.ToLower())
        {
            // Dossier, Subdossier, Dokument
            case "vz":
            case "dokum":
            case "vor":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Vz_OwnSign", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            // Klassifkation / Serie
            case "klas":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Kl_Signatur", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            //Bestand, Teilbestand
            case "tbest":
            case "best":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Bst_Signatur", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;

            //Tektonik / Hauptabteilung
            case "tekt":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Te_Sign", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;
            //Archiv
            case "arch":
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Ar_Kuerz", StringComparison.InvariantCultureIgnoreCase))?.Value.ToString();
                break;
            default:
                throw new ArgumentException($"Level is not supported {levelValue}");
        }

        return MappingFunctions.CreateTextElement(fieldValue, elementName);
    }
    private string MapSignatur(IDictionary<string, object> fieldValues, string levelType)
    {
        string fieldValue;
        switch (levelType.ToLower())
        {
            // Dossier, Subdossier, Dokument
            case "vz":
            case "dokum":
            case "vor":
                fieldValue = fieldValues.ContainsKey("Vz_OwnSign") ? fieldValues["Vz_OwnSign"].ToString() : string.Empty;
                break;

            // Klassifkation / Serie
            case "klas":
                fieldValue = fieldValues.ContainsKey("Kl_Signatur") ? fieldValues["Kl_Signatur"].ToString() : string.Empty;
                break;

            //Bestand, Teilbestand
            case "tbest":
            case "best":
                fieldValue = fieldValues.ContainsKey("Bst_Signatur") ? fieldValues["Bst_Signatur"].ToString() : string.Empty;
                break;

            //Tektonik / Hauptabteilung
            case "tekt":
                fieldValue = fieldValues.ContainsKey("Te_Sign") ? fieldValues["Te_Sign"].ToString() : string.Empty;
                break;
            //Archiv
            case "arch":
                fieldValue = fieldValues.ContainsKey("Ar_Kuerz") ? fieldValues["Ar_Kuerz"].ToString() : string.Empty;
                break;
            default:
                throw new ArgumentException($"Level is not supported {levelType}");
        }

        return fieldValue;
    }

    private int GetIconId(string levelType)
    {
        /*
          abgeleitet aus DocType
            Arch => Archive	
            Tekt => Hauptabteilung
            Best => Bestand
            Tbest => Teilbestand 
            Klas => Serie	
            Vz => Dossier	
            Vor => Subdossier
            Dokum => Dokument
            */
        switch (levelType.ToLower())
        {
            case "arch":
                return 1;
            case "tekt":
                return 2;
            case "best":
                return 3;
            case "tbest":
                return 4;
            case "klas":
                return 5;
            case "vz":
                return 6;
            case "vor":
                return 7;
            // Dossier, Subdossier, Dokument
            case "dokum":
                return 8;
            default:
                throw new ArgumentException($"Level is not supported {levelType}");
        }
    }

    private DataElement MapGeschichteDerUnterlagen(ICollection<DocumentField> fields, string levelValue)
    {
        var fieldValue = string.Empty;
        switch (levelValue.ToLower())
        {
            case "vz":
            case "dokum":
            case "vor": // Dossier, Subdossier, Dokument
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Vz_GeschUnterlagen", StringComparison.InvariantCultureIgnoreCase))?.Value
                    .ToString();
                break;
            //Bestand, Teilbestand
            case "best":
            case "tbest":
                //Bestand, Teilbestand
                fieldValue = fields.FirstOrDefault(f => f.Type.Equals("Bst_Geschichte", StringComparison.InvariantCultureIgnoreCase))?.Value
                    .ToString();
                break;
        }

        return MappingFunctions.CreateTextElement(fieldValue, "GESCHICHTE_DER_VE");
    }

    public ArchiveplanContextItem MapAncestorRecord(IDictionary<string, object> ancestor)
    {
        var archiveplanContextItem = new ArchiveplanContextItem();

        if (ancestor.TryGetValue("doctype", out var levelType))
        {
            archiveplanContextItem.Level = MapStufeText(levelType.ToString());
            archiveplanContextItem.ArchiveRecordId = ancestor["id"].ToString();
            if (ancestor.TryGetValue("Laufzeit", out var laufzeit))
            {
                archiveplanContextItem.DateRangeText = MappingFunctions.FormatLaufzeitText(laufzeit.ToString());
            }
            archiveplanContextItem.RefCode = MapSignatur(ancestor, levelType.ToString());
            archiveplanContextItem.IconId = GetIconId(levelType.ToString());
            // We are not using the title that was returned, because this title is the "kurzname" and not the real title
            archiveplanContextItem.Title = MapTitle(ancestor, levelType.ToString());
        }

        return archiveplanContextItem;
    }

    public SecurityCalculationInputData ExtractSecurityRelevantAttributes(Document document)
    {
        var retVal = new SecurityCalculationInputData();

        if (document != null && document.AdditionalProperties.ContainsKey("type") &&
            document.AdditionalProperties.TryGetValue("type", out var levelObject))
        {
            retVal.DocKey = document.DocKey;
            retVal.Stufe = MapStufeText(levelObject.ToString());
            retVal.BearbeitungsStatus = MappingFunctions.GetFieldValue(document, "Status");

            var laufzeit = MappingFunctions.MapDateTimeElement(document.Block.Fields, "Laufzeit", "ENTSTEHUNGSZEITRAUM");
            if (laufzeit != null)
            {
                retVal.EntstehungszeitraumBis = laufzeit.ElementValue[0].DateRange.ToDate;
            }

            var schutzfrist = MappingFunctions.MapDateTimeElement(document.Block.Fields, "Vz_Schutzfristende", "Vz_Schutzfristdauer");
            if (schutzfrist != null)
            {
                // If time extracted is 00:00:00 then change it to 23:59:59
                retVal.SchutzfristEnde = schutzfrist.ElementValue[0].DateRange.ToDate.TimeOfDay == new TimeSpan(0)
                    ? schutzfrist.ElementValue[0].DateRange.ToDate.AddDays(1).AddSeconds(-1)
                    : schutzfrist.ElementValue[0].DateRange.ToDate;
            }


            retVal.MetadatenPublizierbar = ExtractMetadatenPublizierbar(document, levelObject.ToString());

            retVal.Publikationsrechte = MappingFunctions.GetFieldValue(document.Block.Fields, "Ablieferung_Gp", "Ablieferung_Publikationsrechte");
            retVal.Schutzfristkategorie = MappingFunctions.GetFieldValue(document, "Vz_Schutzfristkategorie");
            retVal.Zugaenglichkeit = MappingFunctions.GetFieldValue(document, "Zugaenglichkeit");
            retVal.ZugaenglichkeitGemaessBga = MappingFunctions.GetFieldValue(document, "Vz_ZugaenglichkeitBGA");

            var zustaendigeStellenDictionary = ExtractZustaendigeStellen(document);
            if (zustaendigeStellenDictionary.Any())
            {
                retVal.ZustaendigeStellenKeys.AddRange(zustaendigeStellenDictionary.Select(z => z.Key)); 
            }
        }

        return retVal;
    }

    private static bool ExtractMetadatenPublizierbar(Document document, string stufe)
    {
        switch (stufe.ToLower())
        {
            // Dossier, Subdossier, Dokument
            case "vz":
            case "vor":
            case "dokum":
                return Convert.ToBoolean(MappingFunctions.GetFieldValue(
                    document.Block.Fields, "Ablieferung_Gp",
                    "Ablieferung_InQuerySichtbar")?.Equals("1", StringComparison.InvariantCultureIgnoreCase));


            case "klas":  // Serie 
            case "best":  // Bestand, Teilbestand
            case "tbest": // Teilbestand
            case "tekt":  // Tektonik / Hauptabteilung
            case "arch":  // Archiv
                return Convert.ToBoolean(MappingFunctions.GetFieldValue(
                    document,
                    "MetadatenPubl")?.Equals("Ja", StringComparison.InvariantCultureIgnoreCase));

            default:
                throw new ArgumentException($"Invalid level: {stufe}");
        }
    }

    private static Dictionary<string, string> ExtractZustaendigeStellen(Document document)
    {
        var retVal = new Dictionary<string, string>();

        var ablieferungGroupFields = document.Block.Fields.FirstOrDefault(f => f.Type.Equals("Ablieferung_Gp", StringComparison.InvariantCultureIgnoreCase))?.Fields;
        if (ablieferungGroupFields != null)
        {
            var zustaendigeStellen = MappingFunctions.GetFieldValue(ablieferungGroupFields, "Ablieferung_ZustaendigeStelle");
            // superduper Regel vom BAR.
            // Es scheint so, also ob die Zuständige Stelle so aufgebaut ist, dass der Name kommt, gefolgt von einem Bindestrich und dann der DocKey
            // Und es können mehrere zuständige Stellen geliefert werden, separiert mit ;
            if (!string.IsNullOrEmpty(zustaendigeStellen))
            {
                var parts = zustaendigeStellen.Split(';').Select(s => s.Trim());
                foreach (var part in parts.Where(p => !string.IsNullOrEmpty(p) && p.Length > 45))
                {
                    // Der vordere Teil bis zum Key sind der Name
                    var name = part.Substring(0, part.Length - 45);
                    // Die letzten 44 Stellen sind der Key
                    var key = part.Substring(part.Length - 44);

                    retVal.Add(key, name);
                }
            }
        }

        return retVal;
    }

    public List<SecurityCalculationInputData> ExtractSecurityRelevantAttributes(DocumentAncestorsDTO ancestors)
    {
        var retVal = new List<SecurityCalculationInputData>();

        foreach (var ancestor in ancestors.Ancestors)
        {
            // Only extract
            // Schutzfristend
            // Schutzfristkategorie
            // Entstehungszeitraum
            // Zugänglichkeit gemäss BGA
            var securityDetail = new SecurityCalculationInputData();

            if (ancestor.TryGetValue("id", out var docKey))
            {
                securityDetail.DocKey = docKey.ToString();
            }
            if (ancestor.TryGetValue("Laufzeit", out var laufzeit))
            {
                securityDetail.EntstehungszeitraumBis = MappingFunctions.GetFromBisDate(laufzeit.ToString()).Item2;
            }
            if (ancestor.TryGetValue("Vz_Schutzfristende", out var schutzfristEnde))
            {
                securityDetail.SchutzfristEnde = MappingFunctions.GetFromBisDate(schutzfristEnde.ToString()).Item2;
                // If time extracted is 00:00:00 then change it to 23:59:59
                securityDetail.SchutzfristEnde = securityDetail.SchutzfristEnde?.TimeOfDay == new TimeSpan(0)
                    ? securityDetail.SchutzfristEnde?.AddDays(1).AddSeconds(-1)
                    : securityDetail.SchutzfristEnde;
            }
            if (ancestor.TryGetValue("Vz_Schutzfristkategorie", out var schutzfristKategorie))
            {
                securityDetail.Schutzfristkategorie = schutzfristKategorie.ToString();
            }
            if (ancestor.TryGetValue("Vz_ZugaenglichkeitBGA", out var zugaenglichkeitBga))
            {
                securityDetail.ZugaenglichkeitGemaessBga = zugaenglichkeitBga.ToString();
            }
            if (ancestor.TryGetValue("Zugaenglichkeit", out var zugaenglichkeit))
            {
                securityDetail.Zugaenglichkeit = zugaenglichkeit.ToString();
            }

            if (ancestor.TryGetValue("doctype", out var levelType))
            {
                securityDetail.Stufe = MapStufeText(levelType.ToString());
            }

            retVal.Add(securityDetail);
                
        }

        return retVal;
    }
}