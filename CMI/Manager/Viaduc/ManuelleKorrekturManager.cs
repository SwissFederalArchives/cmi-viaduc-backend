using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Engine.Anonymization;
using Serilog;

namespace CMI.Manager.Viaduc
{
    public class ManuelleKorrekturManager : IManuelleKorrekturManager
    {
        private readonly IManuelleKorrekturAccess dbManuelleKorrekturAccess;
        private readonly ISearchIndexDataAccess dbSearchAccess;
        private readonly IAnonymizationReferenceEngine anonymizationReferenceEngine;

        public ManuelleKorrekturManager(IManuelleKorrekturAccess dbManuelleKorrekturAccess, ISearchIndexDataAccess dbSearchAccess, IAnonymizationReferenceEngine anonymizationReferenceEngine)
        {
            this.dbSearchAccess = dbSearchAccess;
            this.dbManuelleKorrekturAccess = dbManuelleKorrekturAccess;
            this.anonymizationReferenceEngine = anonymizationReferenceEngine;
        }

        public async Task<ManuelleKorrekturDetailItem> GetManuelleKorrektur(int manuelleKorrekturId)
        {
            var manuelleKorrektur = await dbManuelleKorrekturAccess.GetManuelleKorrektur(manuelleKorrekturId);
            var elasticRecord = dbSearchAccess.FindDocumentWithoutSecurity(manuelleKorrektur.VeId.ToString(), false);
            if (elasticRecord == null)
            {
                return null;
            }
            var verweise = elasticRecord.References.Select(r => dbSearchAccess.FindDocumentWithoutSecurity(r.ArchiveRecordId, false)).ToList();
            var elasticRecordChildren = dbSearchAccess.GetChildrenWithoutSecurity(elasticRecord.ArchiveRecordId, true);

            return await Task.FromResult(new ManuelleKorrekturDetailItem
            {
                ArchivplanKontext = elasticRecord.GetArchivePlanContext(),
                UntergeordneteVEs = CovertToArchiveRecordContextItems(elasticRecordChildren),
                VerweiseVEs = CovertToArchiveRecordContextItems(verweise),
                ManuelleKorrektur = manuelleKorrektur

            });
        }

        public Task<ManuelleKorrekturDto> InsertOrUpdateManuelleKorrektur(ManuelleKorrekturDto manuelleKorrektur, string userId)
        {
            return dbManuelleKorrekturAccess.InsertOrUpdateManuelleKorrektur(manuelleKorrektur, userId);
        }

        public async Task DeleteManuelleKorrektur(int manuelleKorrekturId)
        {
            var manuelleKorrektur = await dbManuelleKorrekturAccess.GetManuelleKorrektur(manuelleKorrekturId);
            var mustBeReset = manuelleKorrektur.ManuelleKorrekturFelder.Any(mkf => mkf.Manuell != string.Empty);

            await dbManuelleKorrekturAccess.DeleteManuelleKorrektur(manuelleKorrekturId);

            if (mustBeReset)
            {
                ResetRecordToAISValues(manuelleKorrektur);
            }
        }

        public async Task BatchDeleteManuelleKorrektur(int[] manuelleKorrekturIds)
        {
            foreach (var id in manuelleKorrekturIds)
            {
                await DeleteManuelleKorrektur(id);
            }
        }

        public async Task<Dictionary<string, string>> BatchAddManuelleKorrektur(string[] identifiers, string userId)
        {
            var result = new Dictionary<string, string>();
            var successfullyAdded = false;

            foreach (var id in identifiers)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                // remove whitespace
                string identifier = id.Trim();
                try
                {
                    // Prüfung ob die ID oder Signatur bereits vorhanden ist
                    var check = await dbManuelleKorrekturAccess.CheckCanInsertManuelleKorrektur(identifier);
                    if (!check)
                    {
                        // Signatur (E5330-01#1982/1#1010*) kann nicht als Key verwendet werden
                        result.Add(result.Count.ToString(), $"VE mit der Signatur oder VE-ID '{identifier}' ist in der Liste schon vorhanden.");
                        continue;
                    }
                    var archiveRecord = GetElasticArchiveDbRecord(identifier);
                    if (archiveRecord == null)
                    {
                        // Signatur (E5330-01#1982/1#1010*) kann nicht als Key verwendet werden
                        result.Add(result.Count.ToString(), $"VE mit der Signatur oder VE-ID '{identifier}' wurde nicht gefunden.");
                    }
                    else if (archiveRecord.FieldAccessTokens == null || archiveRecord.FieldAccessTokens.Count == 0)
                    {
                        result.Add(result.Count.ToString(), $"VE mit der Signatur oder VE-ID '{identifier}' ist nicht für die " +
                                                            $"Anonymisierung vorgesehen (z. B. frei zugänglich oder eine Stufe, die nicht anonymisiert wird).");
                    }
                    else if(archiveRecord.ProtectionEndDate == null)
                    {
                        result.Add(result.Count.ToString(), $"VE mit der Signatur oder VE-ID '{identifier}' kann nicht hinzugefügt werden, " +
                                                            $"da kein Schutzfristende vorhanden ist.");
                    }
                    else
                    {
                        await InsertOrUpdateManuelleKorrektur(archiveRecord.ToManuelleKorrektur(), userId);
                        successfullyAdded = true;
                    }
                }
                catch (ArgumentException)
                {
                    result.Add(result.Count.ToString(), $"Für die Signatur '{identifier}' wurden mehr als eine VE gefunden. Bitte fügen Sie die VE über ihre VE-ID hinzu.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "VE mit der Signatur oder VE-ID '{identifier}' gab es einen Fehler: {Message}", identifier, ex.Message);
                    result.Add(result.Count.ToString(), $"VE mit der Signatur oder VE-ID '{identifier}' gab es einen Fehler: {ex.Message}");
                }
            }

            if (result.Count == 0 && !successfullyAdded)
            {
                result.Add("0", $"Die VE(s) mit der/den Signatur(en) oder VE-IDs '{string.Join(", ", identifiers)}' konnten aus unbekannten Gründen nicht korrekt verarbeitet werden.");
            }
            return result;
        }
        public async Task<ManuelleKorrekturDto> PublizierenManuelleKorrektur(int manuelleKorrekturId, string userId)
        {
            var manuelleKorrektur = await dbManuelleKorrekturAccess.Publizieren(manuelleKorrekturId, userId);
            SetRecordToManualValues(manuelleKorrektur);
            return manuelleKorrektur;
        }
        
        private ElasticArchiveDbRecord GetElasticArchiveDbRecord(string id)
        {
            return dbSearchAccess.FindDbDocument(id, false);
        }

        private void ResetRecordToAISValues(ManuelleKorrekturDto manuelleKorrektur)
        {
            var archiveRecord = GetElasticArchiveDbRecord(manuelleKorrektur.VeId.ToString());
            var mustTitleUpdate = false;

            foreach (var feld in manuelleKorrektur.ManuelleKorrekturFelder)
            {
                switch (feld.Feldname)
                {
                    case ManuelleKorrekturFelder.Titel:
                        if (archiveRecord.Title != feld.Automatisch)
                        {
                            mustTitleUpdate = true;
                            archiveRecord.Title = feld.Automatisch;
                        }
                        break;
                    case ManuelleKorrekturFelder.Darin:
                        archiveRecord.WithinInfo = feld.Automatisch;
                        break;
                    case ManuelleKorrekturFelder.VerwandteVe:
                        archiveRecord.SetCustomProperty("verwandteVe", feld.Automatisch);
                        break;
                    case ManuelleKorrekturFelder.ZusatzkomponenteZac1:
                        archiveRecord.SetCustomProperty("zusatzkomponenteZac1", feld.Automatisch);
                        break;
                    case ManuelleKorrekturFelder.BemerkungZurVe:
                        archiveRecord.SetCustomProperty("bemerkungZurVe", feld.Automatisch);
                        break;
                    default:
                        throw new ArgumentException($"ManuelleKorrekturFelder {feld.Feldname} ist unbekannt");
                }
            }
            archiveRecord.IsAnonymized = manuelleKorrektur.AnonymisiertZumErfassungszeitpunk;
            if (mustTitleUpdate)
            {
                anonymizationReferenceEngine.UpdateSelf(archiveRecord);
                anonymizationReferenceEngine.UpdateDependentRecords(archiveRecord);
            }
            dbSearchAccess.UpdateDocument(archiveRecord);
        }

        private void SetRecordToManualValues(ManuelleKorrekturDto manuelleKorrektur)
        {
            var archiveRecord = GetElasticArchiveDbRecord(manuelleKorrektur.VeId.ToString());
            var containBlockNodes = false;
            var mustTitleUpdate = false;
            foreach (var feld in manuelleKorrektur.ManuelleKorrekturFelder)
            {
                switch (feld.Feldname)
                {
                    case ManuelleKorrekturFelder.Titel:
                        if (string.IsNullOrWhiteSpace(feld.Manuell))
                        {
                            containBlockNodes = containBlockNodes || feld.Automatisch.Contains("███");
                            archiveRecord.Title = feld.Automatisch;
                        }
                        else
                        {
                            containBlockNodes = containBlockNodes || feld.Manuell.Contains("███");
                            archiveRecord.Title = feld.Manuell;
                            mustTitleUpdate = true;
                        }
                        break;
                    case ManuelleKorrekturFelder.Darin:
                        if (string.IsNullOrWhiteSpace(feld.Manuell))
                        {
                            containBlockNodes = containBlockNodes || feld.Automatisch.Contains("███");
                            archiveRecord.WithinInfo = feld.Automatisch;
                        }
                        else
                        {
                            containBlockNodes = containBlockNodes || feld.Manuell.Contains("███");
                            archiveRecord.WithinInfo = feld.Manuell;
                        }
                        break;
                    case ManuelleKorrekturFelder.VerwandteVe:
                        if (string.IsNullOrWhiteSpace(feld.Manuell))
                        {
                            containBlockNodes = containBlockNodes || feld.Automatisch.Contains("███");
                            archiveRecord.SetCustomProperty("verwandteVe", feld.Automatisch);
                        }
                        else
                        {
                            containBlockNodes = containBlockNodes || feld.Manuell.Contains("███");
                            archiveRecord.SetCustomProperty("verwandteVe", feld.Manuell);
                        }
                        break;
                    case ManuelleKorrekturFelder.ZusatzkomponenteZac1:
                        if (string.IsNullOrWhiteSpace(feld.Manuell))
                        {
                            containBlockNodes = containBlockNodes || feld.Automatisch.Contains("███");
                            archiveRecord.SetCustomProperty("zusatzkomponenteZac1", feld.Automatisch);
                        }
                        else
                        {
                            containBlockNodes = containBlockNodes || feld.Manuell.Contains("███");
                            archiveRecord.SetCustomProperty("zusatzkomponenteZac1", feld.Manuell);
                        }
                        break;
                    case ManuelleKorrekturFelder.BemerkungZurVe:
                        if (string.IsNullOrWhiteSpace(feld.Manuell))
                        {
                            containBlockNodes = containBlockNodes || feld.Automatisch.Contains("███");
                            archiveRecord.SetCustomProperty("bemerkungZurVe", feld.Automatisch);
                        }
                        else
                        {
                            containBlockNodes = containBlockNodes || feld.Manuell.Contains("███");
                            archiveRecord.SetCustomProperty("bemerkungZurVe", feld.Manuell);
                        }
                        break;
                    default:
                        throw new ArgumentException($"ManuelleKorrekturFelder {feld.Feldname} ist unbekannt");
                }
            }

            if (containBlockNodes && !archiveRecord.IsAnonymized)
            {
                // Bei bisher nicht anonymisierten VEs nicht gesetzt
                archiveRecord.UnanonymizedFields = new UnanonymizedFields
                {
                    ArchiveplanContext = archiveRecord.ArchiveplanContext,
                    ParentContentInfos = archiveRecord.ParentContentInfos,
                    References =  archiveRecord.References,  
                    Title = manuelleKorrektur.ManuelleKorrekturFelder.First(mk => mk.Feldname == ManuelleKorrekturFelder.Titel).Original,
                    WithinInfo = manuelleKorrektur.ManuelleKorrekturFelder.First(mk => mk.Feldname == ManuelleKorrekturFelder.Darin).Original,
                    VerwandteVe = manuelleKorrektur.ManuelleKorrekturFelder.First(mk => mk.Feldname == ManuelleKorrekturFelder.VerwandteVe).Original,
                    BemerkungZurVe = manuelleKorrektur.ManuelleKorrekturFelder.First(mk => mk.Feldname == ManuelleKorrekturFelder.BemerkungZurVe)
                        .Original,
                    ZusatzkomponenteZac1 = manuelleKorrektur.ManuelleKorrekturFelder
                        .First(mk => mk.Feldname == ManuelleKorrekturFelder.ZusatzkomponenteZac1).Original
                };
            }

            archiveRecord.IsAnonymized = containBlockNodes;
            if (mustTitleUpdate)
            {
                anonymizationReferenceEngine.UpdateSelf(archiveRecord);
                anonymizationReferenceEngine.UpdateDependentRecords(archiveRecord);
            }
            dbSearchAccess.UpdateDocument(archiveRecord);
        }


        private IEnumerable<ArchiveRecordContextItem> CovertToArchiveRecordContextItems(IEnumerable<ElasticArchiveRecord> records)
        {
            return records.Where(record => record != null).Select(record => record.ToArchiveRecordContextItem()).ToList();
        }
    }
}
