using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMI.Contract.Common
{
    [DataContract(IsReference = true)]
    public class PrimaerdatenAuftragStatusInfo
    {
        #region Properties

        public int PrimaerdatenAuftragId { get; set; }

        public AufbereitungsArtEnum AufbereitungsArt { get; set; }

        public long? GroesseInBytes { get; set; }

        public AufbereitungsStatusEnum Status { get; set; }

        public AufbereitungsServices Service { get; set; }

        public int VeId { get; set; }

        public int? GeschaetzteAufbereitungszeit { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        #endregion
    }

    public class PrimaerdatenAuftrag : PrimaerdatenAuftragStatusInfo
    {
        #region Navigation Properties

        public List<PrimaerdatenAuftragLog> PrimaerdatenAuftragLogs { get; set; }

        #endregion

        #region Constructors

        public PrimaerdatenAuftrag()
        {
            PrimaerdatenAuftragLogs = new List<PrimaerdatenAuftragLog>();
        }

        public PrimaerdatenAuftrag(int primaerdatenAuftragId, AufbereitungsArtEnum aufbereitungsArt, long? groesseInBytes, int? verarbeitungskanal,
            int? priorisierungsKategorie, AufbereitungsStatusEnum status, AufbereitungsServices service, string packageId, string packageMetadata,
            int veId, bool abgeschlossen, DateTime? abgeschlossenAm, int? geschaetzteAufbereitungszeit, string errorText, string workload,
            DateTime createdOn, DateTime? modifiedOn, List<PrimaerdatenAuftragLog> primaerdatenAuftragLogs)
        {
            PrimaerdatenAuftragId = primaerdatenAuftragId;
            AufbereitungsArt = aufbereitungsArt;
            GroesseInBytes = groesseInBytes;
            Verarbeitungskanal = verarbeitungskanal;
            PriorisierungsKategorie = priorisierungsKategorie;
            Status = status;
            Service = service;
            PackageId = packageId;
            PackageMetadata = packageMetadata;
            VeId = veId;
            Abgeschlossen = abgeschlossen;
            AbgeschlossenAm = abgeschlossenAm;
            GeschaetzteAufbereitungszeit = geschaetzteAufbereitungszeit;
            ErrorText = errorText;
            Workload = workload;
            CreatedOn = createdOn;
            ModifiedOn = modifiedOn;
            PrimaerdatenAuftragLogs = primaerdatenAuftragLogs;
        }

        #endregion

        #region Properties

        public int? Verarbeitungskanal { get; set; }

        public int? PriorisierungsKategorie { get; set; }

        public string PackageId { get; set; }

        public string PackageMetadata { get; set; }

        public bool Abgeschlossen { get; set; }

        public DateTime? AbgeschlossenAm { get; set; }

        public string ErrorText { get; set; }

        public string Workload { get; set; }

        #endregion
    }
}