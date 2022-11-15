namespace CMI.Contract.Messaging
{
    /// <summary>
    /// Is for references in GridControl to other table entries.
    /// sName is displayed for the user and the Id is for the link between the data. 
    /// </summary>
    public class NameAndId
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }
}