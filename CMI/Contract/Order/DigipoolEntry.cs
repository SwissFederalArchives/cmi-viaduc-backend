using System;

namespace CMI.Contract.Order
{
    public class DigipoolEntry
    {
        #region Diese Informationen werden im Digipool angezeigt

        public int OrderItemId { get; set; }

        public string User { get; set; }
        public string UserId { get; set; }
        public string Signatur { get; set; }
        public string DossierTitel { get; set; }

        public DateTime TerminDigitalisierung { get; set; }
        public int Digitalisierunskategorie { get; set; }
        public int Priority { get; set; }

        public string OrderingComment { get; set; }
        public string InternalComment { get; set; }

        public bool HasAufbereitungsfehler { get; set; }

        #endregion


        #region Zusätzlich für Anbindung Vecteur

        public int? VeId { get; set; }
        public string OrderItemComment { get; set; }
        public DateTime OrderDate { get; set; }
        public ApproveStatus ApproveStatus { get; set; }

        #endregion
    }
}