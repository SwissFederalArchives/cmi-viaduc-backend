using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CMI.Contract.Common
{
    public enum ApplicationFeature
    {
        [Description("Benutzer und Rollen-Benutzerverwaltung-Bereich Access-Tokens für AS-Benutzer bearbeiten")]
        BenutzerUndRolleBenutzerverwaltungZustaendigeStelleEdit = 1000001,

        [Description("Auftragsübersicht-Aufträge-Einsehen")]
        AuftragsuebersichtAuftraegeView = 1000002,

        // [Description("Benutzer und Rollen-Benutzervewaltung-Detail bearbeiten")]
        // BenutzerUndRollenBenutzerverwaltungDetailEdit = 10000025,

        [Description("Auftragsübersicht-Einsichtsgesuche-Einsehen")]
        AuftragsuebersichtEinsichtsgesucheView = 10000026,

        [Description("Auftragsübersicht-Einsichtsgesuche-In Vorlage exportieren ausführen")]
        AuftragsuebersichtEinsichtsgesucheInVorlageExportieren = 10000027,

        [Description("Auftragsübersicht-Einsichtsgesuche-Aufträge abbrechen ausführen ")]
        AuftragsuebersichtEinsichtsgesucheKannAbbrechen = 10000028,

        [Description("Auftragsübersicht-Einsichtsgesuche-Aufträge zurücksetzen ausführen")]
        AuftragsuebersichtEinsichtsgesucheKannZuruecksetzen = 10000029,

        [Description("Auftragsübersicht-Aufträge-Freigabekontrolle ausführen")]
        AuftragsuebersichtAuftraegeKannEntscheidFreigabeHinterlegen = 10000030,

        [Description("Auftragsübersicht-Einsichtsgesuche-Entscheid Gesuch hinterlegen ausführen")]
        AuftragsuebersichtEinsichtsgesucheKannEntscheidGesuchHinterlegen = 10000031,

        [Description("Auftragsübersicht-Aufträge-Aushebungsauftrag ausführen")]
        AuftragsuebersichtAuftraegeKannAushebungsauftraegeDrucken = 10000032,

        [Description("Auftragsübersicht-Aufträge-Auftrag ausleihen ausführen")]
        AuftragsuebersichtAuftraegeKannAuftraegeAusleihen = 10000034,

        [Description("Auftragsübersicht-Aufträge-Auftrag abschliessen ausführen")]
        AuftragsuebersichtAuftraegeKannAbschliessen = 10000035,

        [Description("Auftragsübersicht-Einsichtsgesuche-Digitalisierung auslösen ausführen")]
        AuftragsuebersichtEinsichtsgesucheDigitalisierungAusloesenAusfuehren = 10000036,

        [Description("Auftragsübersicht-Aufträge-Feld Art der Arbeit bearbeiten")]
        AuftragsuebersichtAuftraegeArtDerArbeitEdit = 1000006,

        [Description("Auftragsübersicht-Aufträge-Feld Bemerkung zum Dossier bearbeiten")]
        AuftragsuebersichtAuftraegeBemerkungZumDossierEdit = 1000007,

        [Description("Auftragsübersicht-Aufträge-Feld Interne Bemerkung bearbeiten")]
        AuftragsuebersichtAuftraegeInterneBemerkungEdit = 100008,

        [Description("Auftragsübersicht-Aufträge-Feld Personendaten vorhanden bearbeiten")]
        AuftragsuebersichtAuftraegePersonendatenVorhandenEdit = 1000020,

        [Description("Auftragsübersicht-Aufträge-Feld Begründung Verwaltungsausleihe bearbeiten")]
        AuftragsuebersichtAuftraegeBegruendungVerwaltungsausleiheEdit = 1000021,

        [Description("Public-Client-Statistische Seiten bearbeiten")]
        PublicClientVerwaltenStaticContentEdit = 10000059,

        [Description("Auftragsübersicht-Digipool-Einsehen")]
        AuftragsuebersichtDigipoolView = 10000060,

        [Description("Benutzer und Rollen-Benutzerverwaltung-Einsehen")]
        BenutzerUndRolleBenutzerverwaltungView = 10000061,

        [Description("Auftragsübersicht-Aufträge-Auftrag abbrechen ausführen")]
        AuftragsuebersichtAuftraegeKannAbbrechen = 10000062,

        [Description("Auftragsübersicht-Aufträge-Auftrag zurücksetzen ausführen")]
        AuftragsuebersichtAuftraegeKannZuruecksetzen = 10000063,

        [Description("Auftragsübersicht-Aufträge-Versandkontrolle ausführen")]
        AuftragsuebersichtAuftraegeVersandkontrolleAusfuehren = 10000064,

        [Description("Auftragsübersicht-Aufträge-Barcodes einlesen ausführen")]
        AuftragsuebersichtAuftraegeBarcodesEinlesenAusfuehren = 10000065,

        [Description("Auftragsübersicht-Aufträge-Bereich Logistik bearbeiten")]
        AuftragsuebersichtAuftraegeBereichLogistikBearbeiten = 10000066,

        [Description("Auftragsübersicht-Einsichtsgesuche-Bereich Auftragsdaten und Bereich Freigabe bearbeiten")]
        AuftragsuebersichtEinsichtsgesucheBereichAuftragsdatenUndBereichFreigabeBearbeiten = 10000067,

        [Description("Auftragsübersicht-Digipool-Priorisierung anpassen ausführen")]
        AuftragsuebersichtDigipoolPriorisierungAnpassenAusfuehren = 10000068,

        [Description("Benutzer und Rollen-Benutzerverwaltung-Bereich Benutzerdaten bearbeiten")]
        BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten = 10000069,

        [Description("Benutzer und Rollen-Benutzerverwaltung-Feld Benutzerrolle Public Client bearbeiten")]
        BenutzerUndRollenBenutzerverwaltungFeldBenutzerrollePublicClientBearbeiten = 10000070,

        [Description("Benutzer und Rollen-Benutzerverwaltung-Upload ausführen")]
        BenutzerUndRollenBenutzerverwaltungUploadAusfuehren = 10000071,

        [Description("Benutzer und Rollen-Benutzerverwaltung-Herunterladen ausführen")]
        BenutzerUndRollenBenutzerverwaltungHerunterladenAusfuehren = 10000072,

        [Description("Benutzer und Rollen-Benutzerverwaltung-Feld Forschungsgruppe DDS bearbeiten")]
        BenutzerUndRollenBenutzerverwaltungFeldForschungsgruppeDdsBearbeiten = 10000073,

        [Description("Benutzer und Rollen-Benutzerverwaltung-Feld BAR-interne Konsultation bearbeiten")]
        BenutzerUndRollenBenutzerverwaltungFeldBarInterneKonsultationBearbeiten = 10000074,

        [Description("Benutzer und Rollen-Benutzerverwaltung-Feld Downloadbeschränkung bearbeiten")]
        BenutzerUndRollenBenutzerverwaltungFeldDownloadbeschraenkungBearbeiten = 10000075,

        [Description("Benutzer und Rollen-Rollen-Funktionen-Matrix-Einsehen")]
        BenutzerUndRollenRollenFunktionenMatrixEinsehen = 10000076,

        [Description("Behördenzugriff-Zuständige-Stellen-Einsehen")]
        BehoerdenzugriffZustaendigeStellenEinsehen = 10000077,

        [Description("Behördenzugriff-Zuständige-Stellen-Bearbeiten")]
        BehoerdenzugriffZustaendigeStellenBearbeiten = 10000078,

        [Description("Behördenzugriff-Access-Tokens-Einsehen")]
        BehoerdenzugriffAccessTokensEinsehen = 10000079,

        [Description("Behördenzugriff-Access-Tokens-Bearbeiten")]
        BehoerdenzugriffAccessTokensBearbeiten = 10000080,

        [Description("Administration-Einstellungen-Einsehen")]
        AdministrationEinstellungenEinsehen = 10000081,

        [Description("Administration-Einstellungen-Bearbeiten")]
        AdministrationEinstellungenBearbeiten = 10000082,

        [Description("Administration-News-Einsehen")]
        AdministrationNewsEinsehen = 10000083,

        [Description("Administration-News-Bearbeiten")]
        AdministrationNewsBearbeiten = 10000084,

        [Description("Administration-Systemstatus-Einsehen")]
        AdministrationSystemstatusEinsehen = 10000085,

        [Description("Auftragsübersicht-Digipool-Aufbereitungsfehler zurücksetzen")]
        AuftragsuebersichtDigipoolAufbereitungsfehlerZuruecksetzen = 10000086,

        [Description("Administration-Loginformationen-einsehen")]
        AdministrationLoginformationenEinsehen = 10000087,

        [Description("Auftragsübersicht-Aufträge-Feld Gebrauchskopie Status bearbeiten")]
        AuftragsuebersichtAuftraegeGebrauchskopieStatusEdit = 10000088,

        [Description("Auftragsübersicht-Aufträge-Download Gebrauchskopie ausführen")]
        AuftragsuebersichtAuftraegeKannDownloadGebrauchskopieAusfuehren = 10000089,

        [Description("Auftragsübersicht-Aufträge-[Nicht sichtbare] Daten einsehen ausführen")]
        AuftragsuebersichtAuftraegeViewNichtSichtbar = 10000090,

        [Description("Auftragsübersicht-Einsichtsgesuche-[Nicht sichtbare] Daten einsehen ausführen")]
        AuftragsuebersichtEinsichtsgesucheViewNichtSichtbar = 10000091,

        [Description("Benutzer und Rollen-Benutzerverwaltung-Feld Digitalisierungsbeschränkung bearbeiten")]
        BenutzerUndRollenBenutzerverwaltungFeldDigitalisierungsbeschraenkungBearbeiten = 10000092,

        [Description("Auftragsübersicht-Aufträge-Auftrag reponieren ausführen")]
        AuftragsuebersichtAuftraegeKannReponieren = 10000093,

        [Description("Auftragsübersicht-Aufträge-Mahnung versenden ausführen")]
        AuftragsuebersichtAuftraegeMahnungVersenden = 10000094,

        [Description("Auftragsübersicht-Aufträge-Erinnerung versenden ausführen")]
        AuftragsuebersichtAuftraegeErinnerungVersenden = 10000095,

        [Description("Reporting-Statistiken und Reports-einsehen")]
        ReportingStatisticsReportsEinsehen = 10000096,

        [Description("Reporting-Abbyy Aktivitäten-Einsehen")]
        ReportingStatisticsConverterProgressEinsehen = 10000097
    }

    public class ApplicationFeatureInfo
    {
        public int Id { get; set; }
        public string Identifier { get; set; }

        public string Name { get; set; }
    }

    public static class ApplicationFeatures
    {
        static ApplicationFeatures()
        {
            ApplicationFeaturesById = new Dictionary<int, ApplicationFeature>();
            ApplicationFeaturesByIdentifier = new Dictionary<string, ApplicationFeature>();

            var features = Enum.GetValues(typeof(ApplicationFeature)).Cast<ApplicationFeature>().ToList();

            ApplicationFeaturesById = features.ToDictionary(f => (int) f, f => f);
            ApplicationFeaturesByIdentifier = features.ToDictionary(f => f.ToString(), f => f);

            ApplicationFeatureInfos = features.Select(f => new ApplicationFeatureInfo
            {
                Id = (int) f,
                Identifier = f.ToString(),
                Name = GetEnumDescription(f)
            }).ToList();

            ApplicationFeatureInfosByFeature = ApplicationFeatureInfos.ToDictionary(i => (ApplicationFeature) i.Id, i => i);
        }

        public static Dictionary<int, ApplicationFeature> ApplicationFeaturesById { get; }
        public static Dictionary<string, ApplicationFeature> ApplicationFeaturesByIdentifier { get; }

        public static List<ApplicationFeatureInfo> ApplicationFeatureInfos { get; }
        public static Dictionary<ApplicationFeature, ApplicationFeatureInfo> ApplicationFeatureInfosByFeature { get; }

        private static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[]) fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }

    public static class ApplicationFeaturesExtensions
    {
        public static ApplicationFeatureInfo ToInfo(this ApplicationFeature feature)
        {
            return ApplicationFeatures.ApplicationFeatureInfosByFeature[feature];
        }

        public static IEnumerable<ApplicationFeatureInfo> ToInfos(this IEnumerable<ApplicationFeature> features)
        {
            return features.Select(f => f.ToInfo());
        }
    }
}