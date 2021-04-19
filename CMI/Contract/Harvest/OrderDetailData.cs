namespace CMI.Contract.Harvest
{
    public class OrderDetailData
    {
        public string Title { get; set; }
        public string ReferenceCode { get; set; }
        public string Level { get; set; }
        public string CreationPeriod { get; set; }
        public string BeginStandardDate { get; set; }
        public bool BeginApproxIndicator { get; set; }
        public string EndStandardDate { get; set; }
        public bool EndApproxIndicator { get; set; }
        public int? DateOperatorId { get; set; }
        public string DossierCode { get; set; }
        public string FormerDossierCode { get; set; }
        public string Zusatzkomponente { get; set; }
        public string WithinRemark { get; set; }
        public string Form { get; set; }
        public string Id { get; set; }
    }
}