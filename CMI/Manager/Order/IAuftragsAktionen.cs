using System;
using CMI.Contract.Order;

namespace CMI.Manager.Order
{
    /// <summary>
    ///     Aktionen, welche auf dem Auftrag ausgeführt werden können,
    ///     oder auf einer Menge von Aufträgen.
    /// </summary>
    public interface IAuftragsAktionen
    {
        void InVorlageExportieren(Vorlage vorlage, string sprache);

        void Abbrechen(Abbruchgrund abbruchgrund,
            string bemerkungZumDossier,
            string interneBemerkung);

        void Zuruecksetzen();


        void EntscheidFreigabeHinterlegen(ApproveStatus entscheid, DateTime? datumBewilligung, string interneBemerkung);
        void EntscheidGesuchHinterlegen(EntscheidGesuch entscheid, DateTime datumEntscheid, string interneBemerkung);

        void AushebungsauftragDrucken();
        void Ausleihen();
        void AuftragAusleihen();

        void Abschliessen();
        void Bestellen();
        void SetStatusAushebungBereit();
        void SetStatusDigitalisierungExtern();
        void SetStatusDigitalisierungAbgebrochen(string grund);
        void SetStatusZumReponierenBereit();
    }
}