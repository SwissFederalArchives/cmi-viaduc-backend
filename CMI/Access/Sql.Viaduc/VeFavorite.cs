using System;

namespace CMI.Access.Sql.Viaduc
{
    public class VeFavorite : IFavorite
    {
        public int VeId { get; set; }
        public string ReferenceCode { get; set; }
        public bool CanBeOrdered { get; set; }
        public bool CanBeDownloaded { get; set; }
        public string CreationPeriod { get; set; }
        public string Level { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public FavoriteKind Kind { get; set; }
    }
}