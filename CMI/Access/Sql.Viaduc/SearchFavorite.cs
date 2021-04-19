using System;

namespace CMI.Access.Sql.Viaduc
{
    public class SearchFavorite : IFavorite
    {
        public string Url { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public FavoriteKind Kind { get; set; }
    }
}