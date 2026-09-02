using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace CMI.Access.Harvest.ActaPro.Security
{
    public class AccessTokenProvider
    {
        public List<string> GetMetadataAccessTokens(SecurityCalculationInputData securityData)
        {
            // Nr  Regel                                Wert MetadataAccessTokens
            // 0a  "Status" = "In Bearbeitung"	        leer
            // 0b  "Zugänglichkeit" = "Verboten"	    leer
            // 0c  (Stufe = Subdossier ODER Stufe =     leer
            //      Dokument) UND ("Synchronisierung
            //      Online-Zugang"="Nein" ODER Feld
            //      ist nicht vorhanden
            //      oder hat Leerwert)
            // 0d   Stufe = Datei                       leer
            // 1   "Metadaten publizierbar" = Nein	    "BAR, AS, BVW, Ö3"
            // 2   Alle anderen Fälle 	                "BAR, AS, BVW, Ö3, Ö2, Ö1"

            // Rule 0a
            if (string.IsNullOrEmpty(securityData.BearbeitungsStatus) || securityData.BearbeitungsStatus.Equals(ActaProClientValues.StatusInBearbeitung, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0b
            if (!string.IsNullOrEmpty(securityData.Zugaenglichkeit) && securityData.Zugaenglichkeit.Equals(ActaProClientValues.ZugaenglichkeitVerboten, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0c
            if ((securityData.Stufe.Equals(ActaProClientValues.StufeSubdossier, StringComparison.InvariantCultureIgnoreCase) ||
                securityData.Stufe.Equals(ActaProClientValues.StufeDokument, StringComparison.InvariantCultureIgnoreCase)) &&
                (string.IsNullOrEmpty(securityData.SynchronisationOnlineZugang) || 
                 securityData.SynchronisationOnlineZugang.Equals("Nein", StringComparison.InvariantCultureIgnoreCase)))
            {
                return new List<string>();
            }

            // Rule 0d
            if (securityData.Stufe.Equals(ActaProClientValues.StufeDatei, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 1
            if (securityData.MetadatenPublizierbar == false)
            {
                return ["BAR", "AS", "BVW", "Ö3"];
            }

            // Rule 2
            return ["BAR", "AS", "BVW", "Ö3", "Ö2", "Ö1"];
        }

        public List<string> GetFieldAccessTokens(SecurityCalculationInputData securityData)
        {
            // Nr 	Regel	                            Wert FieldSecurityAccessTokens
            // 0a	"Status" = "In Bearbeitung"	        Leer
            // 0b	"Zugänglichkeit" = "Verboten"	    Leer
            // 0c  (Stufe = Subdossier ODER Stufe =     leer
            //      Dokument) UND ("Synchronisierung
            //      Online-Zugang"="Nein" ODER Feld
            //      ist nicht vorhanden
            //      oder hat Leerwert)
            // 0d   Stufe = Datei                       leer
            // 1	Dokumentype = Archiv, Tektonik,
            //                    Bestand, Teilbestand
            //                    oder Klassifikation	Leer
            // 2	"Zugänglichkeit" = "Gesperrt" UND
            //       die Schutzfrist ist nicht
            //       abgelaufen oder leer	            "BAR, AS_{Partner_Key}"
            // 3	Alle anderen Fälle	                Leer
            // 

            // Rule 0a
            if (string.IsNullOrEmpty(securityData.BearbeitungsStatus) || securityData.BearbeitungsStatus.Equals(ActaProClientValues.StatusInBearbeitung, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0b
            if (!string.IsNullOrEmpty(securityData.Zugaenglichkeit) && securityData.Zugaenglichkeit.Equals(ActaProClientValues.ZugaenglichkeitVerboten, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0c
            if ((securityData.Stufe.Equals(ActaProClientValues.StufeSubdossier, StringComparison.InvariantCultureIgnoreCase) ||
                 securityData.Stufe.Equals(ActaProClientValues.StufeDokument, StringComparison.InvariantCultureIgnoreCase)) &&
                (string.IsNullOrEmpty(securityData.SynchronisationOnlineZugang) ||
                 securityData.SynchronisationOnlineZugang.Equals("Nein", StringComparison.InvariantCultureIgnoreCase)))
            {
                return new List<string>();
            }

            // Rule 0d
            if (securityData.Stufe.Equals(ActaProClientValues.StufeDatei, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 1
            switch (securityData.Stufe)
            {
                case ActaProClientValues.StufeArchiv:
                case ActaProClientValues.StufeHauptabteilung:
                case ActaProClientValues.StufeTeilbestand:
                case ActaProClientValues.StufeBestand:
                case ActaProClientValues.StufeSerie:
                    return new List<string>();
            }

            // Rule 2
            if (securityData.Zugaenglichkeit.Equals(ActaProClientValues.ZugaenglichkeitGesperrt, StringComparison.InvariantCultureIgnoreCase) &&
                (securityData.SchutzfristEnde == null || securityData.SchutzfristEnde > DateTime.Today))
            {
                return GetBarAndPersonalizedAsTokens(securityData);
            }

            // Rule 3
            return new List<string>();
        }

        public List<string> GetDownloadAccessTokens(SecurityCalculationInputData securityData)
        {
            // Nr 	Regel	                                    Wert DownloadAccessToken
            // 0a   Stufen oberhalb Dossier                     <leer>
            // 0b   "Status" = "In Bearbeitung"                 <leer>
            // 0c   "Zugänglichkeit" = "Verboten"               <leer>
            // 0d  (Stufe = Subdossier ODER Stufe =             <leer>
            //      Dokument) UND ("Synchronisierung
            //      Online-Zugang"="Nein" ODER Feld
            //      ist nicht vorhanden
            //      oder hat Leerwert)
            // 1	"Vz_Schutzfristende" liegt in der Zukunft
            //      oder ist leer	                            "BAR, AS_{Partner_Key}"
            // 2	"Ablieferung_Publikationsrechte"
            //      = "Dritte", "Prüfung nötig"
            //      oder "Unbekannt"	                        "BAR, AS_{Partner_Key}"
            // 3	"Zugänglichkeit gemäss
            //      BGA" = "Frei zugänglich"	                "Ö2, Ö3, BVW, AS, AS_{Partner_Key}, BAR,
            // 4	Alle anderen Fälle	                        "BAR, AS_{Partner_Key}"
            // 

            // Rule 0a
            if (securityData.Stufe.Equals(ActaProClientValues.StufeDossier, StringComparison.InvariantCultureIgnoreCase) == false &&
                securityData.Stufe.Equals(ActaProClientValues.StufeSubdossier, StringComparison.InvariantCultureIgnoreCase) == false &&
                securityData.Stufe.Equals(ActaProClientValues.StufeDokument, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                return new List<string>();
            }

            // Rule 0b
            if (string.IsNullOrEmpty(securityData.BearbeitungsStatus) || securityData.BearbeitungsStatus.Equals(ActaProClientValues.StatusInBearbeitung, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0c
            if (!string.IsNullOrEmpty(securityData.Zugaenglichkeit) && securityData.Zugaenglichkeit.Equals(ActaProClientValues.ZugaenglichkeitVerboten, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0d
            if ((securityData.Stufe.Equals(ActaProClientValues.StufeSubdossier, StringComparison.InvariantCultureIgnoreCase) ||
                 securityData.Stufe.Equals(ActaProClientValues.StufeDokument, StringComparison.InvariantCultureIgnoreCase)) &&
                (string.IsNullOrEmpty(securityData.SynchronisationOnlineZugang) ||
                 securityData.SynchronisationOnlineZugang.Equals("Nein", StringComparison.InvariantCultureIgnoreCase)))
            {
                return new List<string>();
            }

            // Rule 1
            if (securityData.SchutzfristEnde.HasValue == false || securityData.SchutzfristEnde.Value > DateTime.Today)
            {
                return GetBarAndPersonalizedAsTokens(securityData);
            }

            // Rule 2
            if (!string.IsNullOrEmpty(securityData.Publikationsrechte) &&
                (securityData.Publikationsrechte.Equals(ActaProClientValues.PublikationsrechteDritte,
                    StringComparison.InvariantCultureIgnoreCase) ||
                securityData.Publikationsrechte.Equals(ActaProClientValues.PublikationsrechtePruefungNoetig,
                    StringComparison.InvariantCultureIgnoreCase) ||
                securityData.Publikationsrechte.Equals(ActaProClientValues.PublikationsrechteUnbekannt,
                    StringComparison.InvariantCultureIgnoreCase)))
            {
                return GetBarAndPersonalizedAsTokens(securityData);
            }

            // Rule 3
            if (securityData.ZugaenglichkeitGemaessBga.Equals(ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                var retVal = new List<string> { "BAR", "AS", "BVW", "Ö3", "Ö2" };
                if (securityData.ZustaendigeStellenKeys is {Count: > 0})
                {
                    retVal.AddRange(securityData.ZustaendigeStellenKeys.Select(key => $"AS_{key}"));
                }
                return retVal;
            }

            // Rule 4
            return GetBarAndPersonalizedAsTokens(securityData);
        }

        public List<string> GetFulltextAccessTokens(SecurityCalculationInputData securityData, List<SecurityCalculationInputData> ancestors)
        {
            // Nr 	Regel	                                        Wert FulltextAcces-Tokens
            // 0a   Stufen oberhalb Dossier                         <leer>
            // 0b   "Status" = "In Bearbeitung"                     <leer>
            // 0c   "Zugänglichkeit" = "Verboten"                   <leer>
            // 0d   (Stufe = Subdossier ODER Stufe =                <leer>
            //      Dokument) UND ("Synchronisierung
            //      Online-Zugang"="Nein" ODER Feld
            //      ist nicht vorhanden
            //      oder hat Leerwert)
            // 1	"Vz_Schutzfristende" liegt in der Zukunft
            //      oder ist leer	                                "BAR, AS_{Partner_Key}"
            // 2	Schutzfristkategorie VE ist NICHT
            //      Art. 9.2 BGA UND
            //      Bis-Entstehungszeitraum liegt weniger weit
            //      als 50 Jahre zurück (jünger als 50 Jahre)	    "BAR, AS_{Partner_Key}"
            // 3	"Zugänglichkeit gemäss
            //      BGA" = "Frei zugänglich" 	                    "Ö2, Ö3, BVW, AS, BAR"
            //                                                      Weiter bei 5

            // 4	Alle anderen Fälle	"BAR, AS_{Partner_Key}"
            // 5	Stufe Dossier	                                Wert aus Schritt 3
            // 6	Ermitteln des Access-Tokens für das
            //      Vater-Dossiers des Sub-dossiers oder Dokuments
            //      Access-Tokens des Vater-Dossiers
            // 

            var retVal = new List<string>();

            Log.Debug("GetFulltextAccessTokens: Stufe={Stufe}, BearbeitungsStatus={BearbeitungsStatus}, Zugaenglichkeit={Zugaenglichkeit}, SynchronisationOnlineZugang={SynchronisationOnlineZugang}, SchutzfristEnde={SchutzfristEnde}, Schutzfristkategorie={Schutzfristkategorie}, EntstehungszeitraumBis={EntstehungszeitraumBis}, ZugaenglichkeitGemaessBga={ZugaenglichkeitGemaessBga}",
                    securityData.Stufe, securityData.BearbeitungsStatus, securityData.Zugaenglichkeit, securityData.SynchronisationOnlineZugang, securityData.SchutzfristEnde, securityData.Schutzfristkategorie, securityData.EntstehungszeitraumBis, securityData.ZugaenglichkeitGemaessBga);

            // Rule 0a
            if (securityData.Stufe.Equals(ActaProClientValues.StufeDossier, StringComparison.InvariantCultureIgnoreCase) == false &&
                securityData.Stufe.Equals(ActaProClientValues.StufeSubdossier, StringComparison.InvariantCultureIgnoreCase) == false &&
                securityData.Stufe.Equals(ActaProClientValues.StufeDokument, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                Log.Debug("Return fulltext access tokens according to rule 0a");
                return retVal;
            }

            // Rule 0b
            if (string.IsNullOrEmpty(securityData.BearbeitungsStatus) || securityData.BearbeitungsStatus.Equals(ActaProClientValues.StatusInBearbeitung, StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Debug("Return fulltext access tokens according to rule 0b");
                return new List<string>();
            }

            // Rule 0c
            if (!string.IsNullOrEmpty(securityData.Zugaenglichkeit) && securityData.Zugaenglichkeit.Equals(ActaProClientValues.ZugaenglichkeitVerboten, StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Debug("Return fulltext access tokens according to rule 0c");
                return new List<string>();
            }

            // Rule 0d
            if ((securityData.Stufe.Equals(ActaProClientValues.StufeSubdossier, StringComparison.InvariantCultureIgnoreCase) ||
                 securityData.Stufe.Equals(ActaProClientValues.StufeDokument, StringComparison.InvariantCultureIgnoreCase)) &&
                (string.IsNullOrEmpty(securityData.SynchronisationOnlineZugang) ||
                 securityData.SynchronisationOnlineZugang.Equals("Nein", StringComparison.InvariantCultureIgnoreCase)))
            {
                Log.Debug("Return fulltext access tokens according to rule 0d");
                return new List<string>();
            }

            // Rule 1
            if (securityData.SchutzfristEnde.HasValue == false || securityData.SchutzfristEnde.Value > DateTime.Today)
            {
                Log.Debug("Return fulltext access tokens according to rule 1");
                return GetBarAndPersonalizedAsTokens(securityData);
            }

            // Rule 2
            if (securityData.Schutzfristkategorie.Equals(ActaProClientValues.SchutzfristKategorieArt92,
                    StringComparison.InvariantCultureIgnoreCase) == false &&
                securityData.EntstehungszeitraumBis.HasValue &&
                securityData.EntstehungszeitraumBis.Value > DateTime.Today.AddYears(-50))
            {
                Log.Debug("Return fulltext access tokens according to rule 2");
                return GetBarAndPersonalizedAsTokens(securityData);
            }

            // Rule 3
            if (securityData.ZugaenglichkeitGemaessBga.Equals(ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Debug("Set fulltext access tokens according to rule 3");
                retVal = ["BAR", "AS", "BVW", "Ö3", "Ö2"];
            }
            else
            {
                // Rule 4: All other cases
                Log.Debug("Return fulltext access tokens according to rule 4");
                return GetBarAndPersonalizedAsTokens(securityData);
            }

            // Rule 5: If it's a dossier, we are done
            if (securityData.Stufe.Equals(ActaProClientValues.StufeDossier, StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Debug("Return fulltext access tokens according to rule 5. Values: {retVal}", string.Join(", ", retVal));
                return retVal;
            }

            // Rule 6: Get Tokens from parent dossier
            var dossierSecurityDetails = ancestors.FirstOrDefault(a =>
                a.Stufe.Equals(ActaProClientValues.StufeDossier,
                    StringComparison.InvariantCultureIgnoreCase));

            if (dossierSecurityDetails != null)
            {
                Log.Debug("Getting fulltext access tokens according to rule 6 (from parent dossier)");
                retVal = GetFulltextAccessTokens(dossierSecurityDetails, ancestors);
                Log.Debug("Return fulltext access tokens according to rule 6. Values: {retVal}", string.Join(", ", retVal));
                return retVal;
            }

            // Should not get here, but if we do return restricted data
            Log.Debug("Return fulltext access tokens according to rule 7.");
            return GetBarAndPersonalizedAsTokens(securityData);

        }


        private static List<string> GetBarAndPersonalizedAsTokens(SecurityCalculationInputData securityData)
        {
            var retVal = new List<string> { "BAR" };
            retVal.AddRange(securityData.ZustaendigeStellenKeys.Select(key => $"AS_{key}"));

            return retVal;
        }

    }
}
