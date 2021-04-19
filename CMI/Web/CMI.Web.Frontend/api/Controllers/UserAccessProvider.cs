using CMI.Access.Sql.Viaduc;

namespace CMI.Web.Frontend.api.Controllers
{
    public class UserAccessProvider : IUserAccessProvider
    {
        private readonly IUserDataAccess userDataAccess;

        public UserAccessProvider(IUserDataAccess userDataAccess)
        {
            this.userDataAccess = userDataAccess;
        }

        public UserAccess GetUserAccess(string language, string userId)
        {
            string accessTokens = null;
            var researcherGroup = false;

            if (userId != null)
            {
                var user = userDataAccess.GetUser(userId);

                if (user != null)
                {
                    accessTokens = user.Access.RolePublicClient;
                    researcherGroup = user.ResearcherGroup;
                }
            }

            return new UserAccess(
                userId,
                accessTokens,
                null, userDataAccess.GetAsTokensDesUser(userId),
                researcherGroup,
                language
            );
        }
    }
}