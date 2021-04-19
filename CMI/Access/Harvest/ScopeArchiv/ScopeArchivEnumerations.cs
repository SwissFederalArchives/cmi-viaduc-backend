namespace CMI.Access.Harvest.ScopeArchiv
{
    /// <summary>
    ///     A list with the reserved data elements in scopeArchiv.
    ///     Usually the reserved data elements have specific meanings and logic.
    /// </summary>
    public enum ScopeArchivDatenElementId
    {
        Titel = 1,
        Signatur,
        SignaturArchivplan,
        Geburtsdatum,
        Todesdatum,
        Stufe,
        Entstehungszeitraum,
        Darin,
        Vorschaubild,
        Ansichtsbild,
        Bearbeitungsstatus,
        ZeitraumMaterialZusammenstellung,
        Findhilfsmittel,
        Zugänge,
        Erschliessungsgrad,
        Archivalienart,
        FrühereSignaturen,
        ExistenzZeitraum,
        Abspieldauer,
        Anzahl,
        Laufmeter,
        Sortiertext,
        ArchivGebäude,
        Enthält,
        AnzahlKopien,
        AusleiherKontigent,
        Archivbenutzer,
        ArchivbenutzerSperrcode,
        Ausleihdauer,
        Reproduktionskontingent,
        Kompetenztraeger,
        TaeglichesAusleihkontingenthkontingent,
        AktenfuehrendeStelleLink = 500,
        AktenbildnerProvenienzLink,
        DossierAblieferungLink,
        DossierObjektLink,
        DossierVerzEinheitLink,
        AblieferungLink,
        ObjektLink,
        RegistraturplanLink,
        RubrikLink,
        OrdnungssystemLink,
        OrdnungskomponenteLink,
        UrsprungsVerzEinheit
    }

    /// <summary>
    ///     A list with the possible data element types.
    ///     A data element type is similar to a data type.
    /// </summary>
    public enum ScopeArchivDatenElementTyp
    {
        DateiVerknuepfung = 1,
        Datumsbereich = 2,
        EinzeldatumPraezis = 3,
        FestkommaZahl = 4,
        GanzeZahl = 5,
        JaNein = 6,
        Text = 7,
        Memo = 8,
        Uhrzeit = 10,
        WebHyperlink = 11,
        Zwischentitel = 13,
        Auswahlliste = 14,
        Zugänge = 15,
        Einzeldatum = 16,
        Bild = 17,
        MailLink = 18,
        Verknüpfung = 19,
        Spieldauer = 20,
        AudioVideo = 21
    }

    /// <summary>
    ///     Possible date operators that are used for date ranges
    /// </summary>
    public enum ScopeArchivDateOperator
    {
        Between = 1, // 1   Zwischen    Zwischen (> x und < y)
        FromTo = 2, // 2   Von / Bis   Von/Bis (>= x und <=y)
        After = 3, // 3   >           Nach (>)
        From = 4, // 4   >=          Ab (>=)
        Before = 5, // 5   <           Vor (<)
        To = 6, // 6   <=          Bis (<=)
        SineDato = 7, // 7   ohne        s.d. (sine dato)
        Exact = 8, // 8   Genau (=)   Genau (=)
        None = 9 // 9   k.A.        keine Angabe
    }

    /// <summary>
    ///     A list with the different entity types available in scopeArchiv
    /// </summary>
    public enum ScopeArchivGeschaeftsObjektKlasse
    {
        Dossiers = 1,
        Dokumente = 2,
        Aktenplaene = 3,
        Partner = 4,
        Kompetenzen = 5,
        Objekte = 6,
        Ablieferungen = 7,
        Behaeltnisse = 8,
        Verzeichnungseinheiten = 9,
        Deskriptoren = 10,
        Ausleihen = 11,
        Reproduktionen = 12
    }

    /// <summary>
    ///     The different media types that scopeArchiv can manage
    /// </summary>
    public enum ScopeArchivMediaType
    {
        Audio = 104,
        Video = 105
    }
}