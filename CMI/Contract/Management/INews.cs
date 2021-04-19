namespace CMI.Contract.Management
{
    public interface INews
    {
        string Id { get; set; }
        string FromDate { get; set; }
        string ToDate { get; set; }
        string De { get; set; }
        string En { get; set; }
        string Fr { get; set; }
        string It { get; set; }
        string DeHeader { get; set; }
        string EnHeader { get; set; }
        string FrHeader { get; set; }
        string ItHeader { get; set; }
    }
}