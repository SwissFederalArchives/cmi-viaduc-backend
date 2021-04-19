using System;

namespace CMI.Access.Sql.Viaduc
{
    public interface IFavorite
    {
        int Id { get; set; }
        string Title { get; set; }
        DateTime CreatedAt { get; set; }
        FavoriteKind Kind { get; set; }
    }
}