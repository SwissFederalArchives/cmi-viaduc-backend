using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace CMI.Access.Sql.Viaduc
{
    public interface IApplicationRoleUserDataAccess
    {
        void InsertRoleUser(int roleId, string userId, string modifiedByUserId);

        void RemoveRoleUser(int roleId, string userId, string modifiedByUserId);

        bool InsertRoleUser(string roleIdentifier, string userId);
        bool RemoveRolesUser(string userId, params string[] roleIdentifier);
    }

    public class ApplicationRoleUserDataAccess : DataAccess, IApplicationRoleUserDataAccess
    {
        private readonly string connectionString;

        public ApplicationRoleUserDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void InsertRoleUser(int roleId, string userId, string modifiedByUserId)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = ""
                                      + "IF NOT EXISTS(SELECT * FROM dbo.ApplicationRoleUser WHERE (RoleId = @roleId) AND (UserId = @userId)) BEGIN"
                                      + "  INSERT INTO dbo.ApplicationRoleUser (RoleId,UserId) OUTPUT INSERTED.ID VALUES (@roleId,@userId); "
                                      + "END ELSE BEGIN"
                                      + "  SELECT ID FROM dbo.ApplicationRoleUser WHERE (RoleId = @roleId) AND (UserId = @userId)"
                                      + "END";

                    cmd.AddParameter("roleId", SqlDbType.Int, roleId);
                    cmd.AddParameter("userId", SqlDbType.NVarChar, userId);

                    cmd.AddModifiedDataToCommand(userId, modifiedByUserId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RemoveRoleUser(int roleId, string userId, string modifiedByUserId)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM dbo.ApplicationRoleUser WHERE (RoleId = @roleId) AND (UserId = @userId);";
                    cmd.AddParameter("roleId", SqlDbType.Int, roleId);
                    cmd.AddParameter("userId", SqlDbType.NVarChar, userId);

                    cmd.AddModifiedDataToCommand(userId, modifiedByUserId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool InsertRoleUser(string roleIdentifier, string userId)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = ""
                                      + "IF NOT EXISTS(SELECT * FROM dbo.ApplicationRoleUser WHERE (RoleId = (SELECT TOP 1 ID FROM ApplicationRole WHERE Identifier = @identifier)) AND (UserId = @userId)) BEGIN"
                                      + "  INSERT INTO dbo.ApplicationRoleUser (RoleId,UserId) OUTPUT INSERTED.ID VALUES "
                                      + " ((SELECT TOP 1 ID FROM ApplicationRole WHERE Identifier = @identifier),@userId); "
                                      + "END ELSE BEGIN"
                                      + "  SELECT ID FROM dbo.ApplicationRoleUser WHERE (RoleId = (SELECT TOP 1 ID FROM ApplicationRole WHERE Identifier = @identifier)) AND (UserId = @userId)"
                                      + "END";

                    cmd.AddParameter("identifier", SqlDbType.NVarChar, roleIdentifier);
                    cmd.AddParameter("userId", SqlDbType.NVarChar, userId);

                    var userRoleId = Convert.ToInt32(cmd.ExecuteScalar());

                    return userRoleId > 0;
                }
            }
        }

        public bool RemoveRolesUser(string userId, params string[] roleIdentifierList)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM dbo.ApplicationRoleUser " +
                                      $"WHERE (RoleId IN (SELECT ID FROM ApplicationRole WHERE Identifier IN ({string.Join(",", roleIdentifierList.Select(currRoleId => $"'{currRoleId}'").ToList())}))) " +
                                      "AND (UserId = @userId);";

                    cmd.AddParameter("userId", SqlDbType.NVarChar, userId);
                    var nuRows = cmd.ExecuteNonQuery();
                    return nuRows > 0;
                }
            }
        }
    }
}