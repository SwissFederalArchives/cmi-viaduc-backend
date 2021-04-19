namespace CMI.Contract.Common
{
    public interface IUpdatePrimaerdatenAuftragStatus
    {
        int PrimaerdatenAuftragId { get; set; }
        AufbereitungsStatusEnum Status { get; set; }
        AufbereitungsServices Service { get; set; }
        int? Verarbeitungskanal { get; set; }
        string ErrorText { get; set; }
    }

    public class UpdatePrimaerdatenAuftragStatus : IUpdatePrimaerdatenAuftragStatus
    {
        public int PrimaerdatenAuftragId { get; set; }
        public AufbereitungsStatusEnum Status { get; set; }
        public AufbereitungsServices Service { get; set; }
        public int? Verarbeitungskanal { get; set; }
        public string ErrorText { get; set; }
    }
}