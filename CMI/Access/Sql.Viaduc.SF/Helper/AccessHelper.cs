
using System.Linq;

namespace CMI.Access.Sql.Viaduc.EF.Helper
{
    public class AccessHelper
    {
        private readonly ViaducDb dbContext;
        public AccessHelper(ViaducDb dbContext)
        {
            this.dbContext = dbContext;
        }
        internal string GetUserNameFromId(string userId)
        {
            var result = dbContext.ApplicationUsers.FirstOrDefault(a => a.ID == userId);
            return result == null ? userId : $"{result.FirstName} {result.FamilyName}".Trim();
        }
    }
}
