using CMI.Access.Sql.Viaduc;

namespace CMI.Manager.Order.Status
{
    public class Users
    {
        public static User System { get; } = new User {Id = "System", FamilyName = "System"};

        public static User Vecteur { get; } = new User {Id = "Vecteur", FamilyName = "Vecteur"};

        public static User Migration { get; } = new User {Id = "MigrationUser", FamilyName = "Migration"};

        public static bool IsSystemUser(string userId)
        {
            return userId == System.Id || userId == Vecteur.Id || userId == Migration.Id;
        }
    }
}