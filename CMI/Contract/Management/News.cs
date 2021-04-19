namespace CMI.Contract.Management
{
    public class News : INews
    {
        public string Id { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string De { get; set; }
        public string En { get; set; }
        public string Fr { get; set; }
        public string It { get; set; }
        public string DeHeader { get; set; }
        public string EnHeader { get; set; }
        public string FrHeader { get; set; }
        public string ItHeader { get; set; }
    }
}