using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMI.Access.Harvest.ActaPro.Security
{
    public class AccessTokenProvider
    {
        public List<string> GetMetadataAccessTokens(SecurityCalculationInputData securityData)
        {
            // Nr  Regel                                Wert MetadataAccessTokens
            // 0a  "Status" = "In Bearbeitung"	        leer
            // 0b  "Zugänglichkeit" = "Verboten"	    leer
            // 1   "Metadaten publizierbar" = Nein	    "BAR, AS, BVW, Ö3"
            // 2   Alle anderen Fälle 	                "BAR, AS, BVW, Ö3, Ö2, Ö1"

            // Rule 0a
            if (securityData.BearbeitungsStatus.Equals(ActaProClientValues.StatusInBearbeitung, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0b
            if (!string.IsNullOrEmpty(securityData.Zugaenglichkeit) && securityData.Zugaenglichkeit.Equals(ActaProClientValues.ZugaenglichkeitVerboten, StringComparison.InvariantCultureIgnoreCase))
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
            // 1	Dokumentype = Archiv, Tektonik,
            //                    Bestand, Teilbestand
            //                    oder Klassifikation	Leer
            // 2	"Zugänglichkeit" = "Gesperrt" UND
            //       die Schutzfrist ist nicht
            //       abgelaufen oder leer	            "BAR, AS_{Partner_Key}"
            // 3	Alle anderen Fälle	                Leer
            // 

            // Rule 0a
            if (!string.IsNullOrEmpty(securityData.BearbeitungsStatus) && securityData.BearbeitungsStatus.Equals(ActaProClientValues.StatusInBearbeitung, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0b
            if (!string.IsNullOrEmpty(securityData.Zugaenglichkeit) && securityData.Zugaenglichkeit.Equals(ActaProClientValues.ZugaenglichkeitVerboten, StringComparison.InvariantCultureIgnoreCase))
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
            // 0a    Stufen oberhalb Dossier                     <leer>
            // 0b   "Status" = "In Bearbeitung"                 <leer>
            // 0c   "Zugänglichkeit" = "Verboten"               <leer>
            // 1	"Vz_Schutzfristende" liegt in der Zukunft
            //      oder ist leer	                            "BAR, AS_{Partner_Key}"
            // 2	"Ablieferung_Publikationsrechte"
            //      = "Dritte", "Prüfung nötig"
            //      oder "Unbekannt"	                        "BAR, AS_{Partner_Key}"
            // 3	"Zugänglichkeit gemäss
            //      BGA" = "Frei zugänglich"	                "Ö2, Ö3, BVW, AS, BAR,
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
            if (!string.IsNullOrEmpty(securityData.BearbeitungsStatus) && securityData.BearbeitungsStatus.Equals(ActaProClientValues.StatusInBearbeitung, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0c
            if (!string.IsNullOrEmpty(securityData.Zugaenglichkeit) && securityData.Zugaenglichkeit.Equals(ActaProClientValues.ZugaenglichkeitVerboten, StringComparison.InvariantCultureIgnoreCase))
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
                return ["BAR", "AS", "BVW", "Ö3", "Ö2"];
            }

            // Rule 4
            return GetBarAndPersonalizedAsTokens(securityData);
        }

        public List<string> GetFulltextAccessTokens(SecurityCalculationInputData securityData, List<SecurityCalculationInputData> ancestors)
        {
            // Nr 	Regel	                                        Wert FulltextAcces-Tokens
            // 0a    Stufen oberhalb Dossier                        <leer>
            // 0b   "Status" = "In Bearbeitung"                     <leer>
            // 0c   "Zugänglichkeit" = "Verboten"                   <leer>
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

            // Rule 0a
            if (securityData.Stufe.Equals(ActaProClientValues.StufeDossier, StringComparison.InvariantCultureIgnoreCase) == false &&
                securityData.Stufe.Equals(ActaProClientValues.StufeSubdossier, StringComparison.InvariantCultureIgnoreCase) == false &&
                securityData.Stufe.Equals(ActaProClientValues.StufeDokument, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                return retVal;
            }

            // Rule 0b
            if (!string.IsNullOrEmpty(securityData.BearbeitungsStatus) && securityData.BearbeitungsStatus.Equals(ActaProClientValues.StatusInBearbeitung, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }

            // Rule 0c
            if (!string.IsNullOrEmpty(securityData.Zugaenglichkeit) && securityData.Zugaenglichkeit.Equals(ActaProClientValues.ZugaenglichkeitVerboten, StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>();
            }
            
            // Rule 1
            if (securityData.SchutzfristEnde.HasValue == false || securityData.SchutzfristEnde.Value > DateTime.Today)
            {
                return GetBarAndPersonalizedAsTokens(securityData);
            }

            // Rule 2
            if (securityData.Schutzfristkategorie.Equals(ActaProClientValues.SchutzfristKategorieArt92,
                    StringComparison.InvariantCultureIgnoreCase) == false &&
                securityData.EntstehungszeitraumBis.HasValue &&
                securityData.EntstehungszeitraumBis.Value > DateTime.Today.AddYears(-50))
            {
                return GetBarAndPersonalizedAsTokens(securityData);
            }

            // Rule 3
            if (securityData.ZugaenglichkeitGemaessBga.Equals(ActaProClientValues.ZugaenglichkeitBGAFreiZugaenglich,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                retVal = ["BAR", "AS", "BVW", "Ö3", "Ö2"];
            }
            else
            {
                // Rule 4: All other cases
                return GetBarAndPersonalizedAsTokens(securityData);
            }

            // Rule 5: If it's a dossier, we are done
            if (securityData.Stufe.Equals(ActaProClientValues.StufeDossier, StringComparison.InvariantCultureIgnoreCase))
            {
                return retVal;
            }

            // Rule 6: Get Tokens from parent dossier
            var dossierSecurityDetails = ancestors.FirstOrDefault(a =>
                a.Stufe.Equals(ActaProClientValues.StufeDossier,
                    StringComparison.InvariantCultureIgnoreCase));

            if (dossierSecurityDetails != null)
            {
                retVal = GetFulltextAccessTokens(dossierSecurityDetails, ancestors);
                return retVal;
            }

            // Should not get here, but if we do return restricted data
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
