using System.Collections.Generic;
using CMI.Contract.Common;
using Newtonsoft.Json.Linq;

namespace CMI.Access.Sql.Viaduc
{
    public interface IUserDataAccess
    {
        User GetUser(string userId);

        List<User> GetUsers(string[] userIds);

        User GetUserWitExtId(string userExtId);

        IEnumerable<User> GetAllUsers();

        string[] GetAsTokensDesUser(string userId);

        string[] GetTokensDesUser(string userId);

        void InsertUser(User user);

        void UpdateClaims(string userId, JObject claims);

        void UpdateUserOnLogin(User user, string userId, string modifiedBy);

        void UpdateUserProfile(string userId, User user);

        void UpdateUser(User user, string modifiedByUserId);

        void UpdateUserSetting(JObject setting, string userId);

        void DeleteAblieferdeStelleFromUser(string userId, int ablieferndeStelleId);

        void DeleteAllAblieferdeStelleFromUser(string userId);

        void CleanAndAddAblieferndeStelleToUser(string userId, List<int> ablieferndeStelleIds, string modifiedByUserId);

        byte[] GetIdentifierDocument(string userId);
        void SetIdentifierDocument(string userId, byte[] file, string rolePublicClient);
        string GetRoleForClient(string userId);
        string GetEiamRoles(string userId);

        void StoreDownloadReasonInHistory(ElasticArchiveRecord record, User user, UserAccess access, int reasonId);
    }
}