using System;

namespace CMI.Contract.Order
{
    public class PrimaerdatenReportRecord
    {
        public string AufbereitungsArt { get; set; }
        public int? OrderId { get; set; }
        public int? VeId { get; set; }
        public int? MutationsId { get; set; }
        public int? PrimaerdatenAuftragId { get; set; }
        public float Size { get; set; }
        public int FileCount { get; set; }
        public string FileFormats { get; set; }
        public string Source { get; set; }
        public DateTime? NeuEingegangen { get; set; }
        public DateTime? FreigabePrüfen { get; set; }
        public DateTime? FürDigitalisierungBereit { get; set; }
        public string DauerManuelleFreigabe { get; set; }
        public DateTime? FürAushebungBereit { get; set; }
        public string DauerAuftragsabrufVecteur { get; set; }
        public DateTime? Ausgeliehen { get; set; }
        public string DauerAusleiheLogistik { get; set; }
        public DateTime? ZumReponierenBereit { get; set; }
        public string DauerDigitalisierungVecteur { get; set; }
        public DateTime? PrimaryDataLinkCreationDate { get; set; }
        public string DauerUpdateAIPAdresseAIS { get; set; }
        public DateTime? StartFirstSynchronizationAttempt { get; set; }
        public string DauerStartSynchronisierungWebOZ { get; set; }
        public DateTime? StartLastSynchronizationAttempt { get; set; }
        public DateTime? CompletionLastSynchronizationAttempt { get; set; }
        public int CountSynchronizationAttempts { get; set; }
        public string DauerErfolgreicherSyncVersuch { get; set; }
        public string DauerAlleSyncVersuche { get; set; }
        public string DauerZumReponierenBereitSyncCompleted { get; set; }
        public DateTime? ClickButtonPrepareDigitalCopy { get; set; }
        public string EstimatedPreparationTimeVeAccordingDetailPage { get; set; }
        public DateTime? StartFirstPreparationAttempt { get; set; }
        public DateTime? StartLastPreparationAttempt { get; set; }
        public DateTime? CompletionLastPreparationAttempt { get; set; }
        public int? CountPreparationAttempts { get; set; }
        public string DauerErfolgreicherAufbereitungsversuch { get; set; }
        public string DauerAllAufbereitungsversuch { get; set; }
        public string DauerSyncCompletedAufbereitungErfolgreich { get; set; }
        public DateTime? StorageUseCopyCache { get; set; }
        public string DauerAufbereitungErfolgreichSpeicherungGebrauchskopieCache { get; set; }
        public DateTime? ShippingMailReadForDownload { get; set; }
        public string DauerAufbereitungErfolgreichMailVersandt { get; set; }
        public string EingangBestellungVersandEMailZumDownloadBereit { get; set; }
        public string KlickButtonDigitalisatAufbereitenVersandEMailZumDownloadBereit { get; set; }
    }
}
