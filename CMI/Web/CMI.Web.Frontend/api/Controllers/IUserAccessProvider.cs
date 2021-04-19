using CMI.Access.Sql.Viaduc;

namespace CMI.Web.Frontend.api.Controllers
{
    public interface IUserAccessProvider
    {
        UserAccess GetUserAccess(string language, string userId);
    }
}