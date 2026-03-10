using Newtonsoft.Json.Linq;

namespace CMI.Access.Harvest.ActaPro;

public class ActaProClientValues
{

    // Status
    public const string StatusAbgeschlossen = "abgeschlossen";
    public const string StatusInBearbeitung = "In Bearbeitung";

    // Zugaenglichkeit
    public const string ZugaenglichkeitNichtOeffentlich = "Nicht öffentlich";
    public const string ZugaenglichkeitVerboten = "Verboten";
    public const string ZugaenglichkeitGesperrt = "Gesperrt";
    public const string ZugaenglichkeitUneingeschraenkt = "Uneingeschränkt";

    // Schutzfristkategorie
    public const string SchutzfristKategorieArt91 = "Art. 9.1 BGA";
    public const string SchutzfristKategorieArt92 = "Art. 9.2 BGA";

    // ZugänglichkeitBGA
    public const string ZugaenglichkeitBGAPruefungNoetig = "Prüfung nötig";
    public const string ZugaenglichkeitBGAFreiZugaenglich = "Frei zugänglich";

    // Publikationsrechte
    public const string PublikationsrechteBAR = "BAR";
    public const string PublikationsrechteDritte = "Dritte";
    public const string PublikationsrechtePruefungNoetig = "Prüfung nötig";
    public const string PublikationsrechteUnbekannt = "Unbekannt";

    // Stufen
    public const string StufeArchiv = "Archiv";
    public const string StufeHauptabteilung = "Hauptabteilung";
    public const string StufeTeilbestand = "Teilbestand";
    public const string StufeBestand = "Bestand";
    public const string StufeSerie = "Serie";
    public const string StufeDokument = "Dokument";
    public const string StufeSubdossier = "Subdossier";
    public const string StufeDossier = "Dossier";


    public static string[] AllVerzEinheitDocTypes =
    [
        "Arch", "Tekt", "Best", "Dokum", "Klas", "TBest", "Vor", "Vz"
    ];
}