using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using CMI.Contract.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Access.Sql.Viaduc
{
    public class UserDataAccess : DataAccess, IUserDataAccess
    {
        private readonly string connectionString;

        public UserDataAccess(string connectionString)
        {
            this.connectionString = connectionString;
        }


        private static string Sql => @"SELECT
Id, 
FamilyName, 
FirstName, 
Organization, 
Street, 
StreetAttachment, 
ZipCode, 
Town, 
CountryCode, 
EmailAddress, 
PhoneNumber, 
SkypeName, 
MobileNumber, 
Setting, 
Claims, 
(select 
   Id AS UserId,
   RolePublicClient,
   (
     select Token as string
	 from ApplicationUserAblieferndeStelle R
     INNER JOIN AblieferndeStelle AS s
     ON s.AblieferndeStelleId = R.AblieferndeStelleId
     INNER JOIN AsTokenMapping AS m
     ON m.AblieferndeStelleId = s.AblieferndeStelleId
     INNER JOIN AblieferndeStelleToken t
     ON t.TokenId = m.TokenId
     WHERE r.UserId = ApplicationUser.ID
	 for xml path (''), TYPE
   ) as AsTokens,
   EiamRoles as EiamRole,
   ResearcherGroup as ResearcherGroup,
   [Language] as Language 
from ApplicationUser as UserAccess where UserAccess.ID = ApplicationUser.ID
for xml path ('UserAccess'), TYPE) as Access, 

(select r.ID as Id, Identifier, Name, Description, Created, Updated
from  ApplicationRoleUser as m inner join ApplicationRole r on m.RoleId = r.ID
where m.UserId = ApplicationUser.ID
for xml path('ApplicationRole'), root('ArrayOfApplicationRole'), TYPE) as Roles,

(select f.FeatureId as ""*"" from
ApplicationRoleUser as m inner join ApplicationRoleFeature f on f.RoleId = m.RoleId
where m.UserId = ApplicationUser.ID
for xml path('ApplicationFeature'), root('ArrayOfApplicationFeature'), TYPE) as Features,

(select a.AblieferndeStelleId, a.Bezeichnung, a.Kuerzel, (select t.Token as string from AsTokenMapping m2 inner join AblieferndeStelleToken t on m2.TokenId = t.TokenId where m2.AblieferndeStelleId = a.AblieferndeStelleId for xml path (''), TYPE) as Tokens from ApplicationUserAblieferndeStelle m join AblieferndeStelle a on  m.AblieferndeStelleId = a.AblieferndeStelleId
where m.UserId = ApplicationUser.ID for xml path('AblieferndeStelleDto'), root('ArrayOfAblieferndeStelleDto'), TYPE) as AblieferndeStelleList,
UserExtId, 
Language, 
Created, 
Updated, 
Fulltext, 
Updated, 
CreatedOn, 
CreatedBy, 
ModifiedOn, 
ModifiedBy, 
Birthday, 
FabasoftDossier, 
ReasonForRejection, 
IsIdentifiedUser, 
QoAValue,
HomeName,
LastLoginDate,
RolePublicClient, 
EiamRoles, 
ResearcherGroup, 
BarInternalConsultation, 
(CASE WHEN IdentifierDocument IS NOT NULL THEN 1 ELSE 0 END) AS HasIdentifizierungsmittel,
DownloadLimitDisabledUntil,
DigitalisierungsbeschraenkungAufgehobenBis,
ActiveAspNetSessionId 
FROM ApplicationUser ";

        public User GetUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException(nameof(userId));
            }

            User user = null;

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        $"{Sql} WHERE ID = @p1";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = reader.ToUser<User>(userId);
                        }
                    }
                }
            }

            return user;
        }

        public List<User> GetUsers(string[] userIds)
        {
            var list = new List<User>();
            if (userIds.Length == 0)
            {
                throw new ArgumentException(nameof(userIds));
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        $"{Sql} WHERE ID in ({string.Join(",", userIds.Select(id => $"'{id.Replace("'", "''")}'"))} )";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(reader.ToUser<User>());
                        }
                    }
                }
            }

            return list;
        }

        public User GetUserWitExtId(string userExtId)
        {
            User user = null;

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        $"{Sql} WHERE USEREXTID = @p1";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userExtId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            user = reader.ToUser<User>();
                        }
                    }
                }
            }

            return user;
        }

        public IEnumerable<User> GetAllUsers()
        {
            User user;

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = Sql;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            user = reader.ToUser<User>();
                            yield return user;
                        }
                    }
                }
            }
        }

        public string[] GetAsTokensDesUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new string[0];
            }

            var u = GetUser(userId);
            if (u == null)
            {
                return new string[0];
            }

            return u.Access.AsTokens;
        }

        public string[] GetTokensDesUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new string[0];
            }

            var u = GetUser(userId);
            if (u == null)
            {
                return new string[0];
            }

            return u.Access.CombinedTokens;
        }


        /// <summary>
        ///     Neuen Benutzer anlegen
        /// </summary>
        /// <param name="user"></param>
        public void InsertUser(User user)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.AddParameter("Id", SqlDbType.NVarChar, user.Id);
                    cmd.AddParameter("FamilyName", SqlDbType.NVarChar, user.FamilyName);
                    cmd.AddParameter("FirstName", SqlDbType.NVarChar, user.FirstName);
                    cmd.AddParameter("EmailAddress", SqlDbType.NVarChar, user.EmailAddress);
                    cmd.AddParameter("UserExtId", SqlDbType.NVarChar, user.UserExtId);
                    cmd.AddParameter("Birthday", SqlDbType.DateTime, user.Birthday);
                    cmd.AddParameter("Organization", SqlDbType.NVarChar, user.Organization);
                    cmd.AddParameter("Street", SqlDbType.NVarChar, user.Street);
                    cmd.AddParameter("StreetAttachment", SqlDbType.NVarChar, user.StreetAttachment);
                    cmd.AddParameter("ZipCode", SqlDbType.NVarChar, user.ZipCode);
                    cmd.AddParameter("Town", SqlDbType.NVarChar, user.Town);
                    cmd.AddParameter("CountryCode", SqlDbType.NVarChar, user.CountryCode);
                    cmd.AddParameter("PhoneNumber", SqlDbType.NVarChar, user.PhoneNumber);
                    cmd.AddParameter("Claims", SqlDbType.NVarChar, JsonConvert.SerializeObject(user.Claims, Formatting.Indented));
                    cmd.AddParameter("IsIdentifiedUser", SqlDbType.Bit, user.IsIdentifiedUser);
                    cmd.AddParameter("QoAValue", SqlDbType.Int, user.QoAValue);
                    cmd.AddParameter("HomeName", SqlDbType.NVarChar, user.HomeName);
                    cmd.AddParameter("LastLoginDate", SqlDbType.DateTime, user.LastLoginDate);
                    cmd.AddParameter("RolePublicClient", SqlDbType.NVarChar, user.RolePublicClient);
                    cmd.AddParameter("EiamRoles", SqlDbType.NVarChar, user.EiamRoles);
                    cmd.AddParameter("MobileNumber", SqlDbType.NVarChar, user.MobileNumber);
                    cmd.AddParameter("Language", SqlDbType.NVarChar, string.IsNullOrEmpty(user.Language) ? "de" : user.Language);
                    cmd.AddParameter("CreatedBy", SqlDbType.NVarChar, user.EmailAddress);
                    cmd.AddParameter("ActiveAspNetSessionId", SqlDbType.NVarChar, user.ActiveAspNetSessionId);

                    cmd.CommandText = @"INSERT INTO ApplicationUser (
                        Id, FamilyName, FirstName, EmailAddress, UserExtId,
                        Birthday, Organization, Street, StreetAttachment, ZipCode,
                        Town, CountryCode, PhoneNumber, Claims, IsIdentifiedUser, QoAValue, HomeName, LastLoginDate, Language,
                         RolePublicClient, EiamRoles, MobileNumber, CreatedBy, ActiveAspNetSessionId
                        ) VALUES (
                        @Id, @FamilyName, @FirstName, @EmailAddress, @UserExtId, 
                        @Birthday, @Organization, @Street, @StreetAttachment, @ZipCode, 
                        @Town, @CountryCode, @PhoneNumber, @Claims, @IsIdentifiedUser, @QoAValue, @HomeName, @LastLoginDate, @Language,
                        @RolePublicClient, @EiamRoles, @MobileNumber, @CreatedBy, @ActiveAspNetSessionId)";

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateUserOnLogin(User user, string userId, string modifiedBy)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.AddParameter("Id", SqlDbType.NVarChar, user.Id);
                    cmd.AddParameter("FamilyName", SqlDbType.NVarChar, user.FamilyName);
                    cmd.AddParameter("FirstName", SqlDbType.NVarChar, user.FirstName);
                    cmd.AddParameter("EmailAddress", SqlDbType.NVarChar, user.EmailAddress);
                    cmd.AddParameter("Claims", SqlDbType.NVarChar, JsonConvert.SerializeObject(user.Claims, Formatting.Indented));
                    cmd.AddParameter("UserExtId", SqlDbType.NVarChar, user.UserExtId);
                    cmd.AddParameter("ModifiedOn", SqlDbType.DateTime, DateTime.Now);
                    cmd.AddParameter("ModifiedBy", SqlDbType.NVarChar, modifiedBy);
                    cmd.AddParameter("EiamRoles", SqlDbType.NVarChar, user.EiamRoles);
                    cmd.AddParameter("QoAValue", SqlDbType.Int, user.QoAValue);
                    cmd.AddParameter("HomeName", SqlDbType.NVarChar, user.HomeName);

                    cmd.CommandText =
                        "UPDATE ApplicationUser SET FamilyName = @FamilyName, FirstName = @FirstName, EmailAddress = @EmailAddress, Claims = @Claims, UserExtId = @UserExtId, ModifiedOn = @ModifiedOn, ModifiedBy = @ModifiedBy, EiamRoles = @EiamRoles, QoAValue = @QoAValue, HomeName = @HomeName WHERE ID = @Id;";

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateClaims(string userId, JObject claims)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE ApplicationUser SET Claims = @p2 WHERE ID = @p1;";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = JsonConvert.SerializeObject(claims, Formatting.Indented).ToDbParameterValue(),
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateUserProfile(string userId, User user)
        {
            var currentUser = GetUser(userId);
            var isOe2 = currentUser.RolePublicClient == AccessRoles.RoleOe2;
            var isOe3 = currentUser.RolePublicClient == AccessRoles.RoleOe3;
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var optionalCommands = string.Empty;
                    cmd.AddParameter("Id", SqlDbType.NVarChar, userId);
                    cmd.AddParameter("organization", SqlDbType.NVarChar, user.Organization.ToDbParameterValue());
                    cmd.AddParameter("street", SqlDbType.NVarChar, user.Street.ToDbParameterValue());
                    cmd.AddParameter("streetAttachment", SqlDbType.NVarChar, user.StreetAttachment.ToDbParameterValue());
                    cmd.AddParameter("zipCode", SqlDbType.NVarChar, user.ZipCode.ToDbParameterValue());
                    cmd.AddParameter("town", SqlDbType.NVarChar, user.Town.ToDbParameterValue());
                    cmd.AddParameter("countryCode", SqlDbType.NVarChar, user.CountryCode.ToDbParameterValue());
                    cmd.AddParameter("phoneNumber", SqlDbType.NVarChar, user.PhoneNumber.ToDbParameterValue());
                    cmd.AddParameter("skypeName", SqlDbType.NVarChar, user.SkypeName.ToDbParameterValue());
                    cmd.AddParameter("modifiedOn", SqlDbType.DateTime, DateTime.Now.ToDbParameterValue());
                    cmd.AddParameter("emailAddress", SqlDbType.NVarChar, user.EmailAddress.ToDbParameterValue());
                    cmd.AddParameter("language", SqlDbType.NVarChar, user.Language.ToDbParameterValue());
                    // Mobiltelefon dient nur zur Kommunikation, somit von jedem Benutzer änderbar.
                    cmd.AddParameter("mobileNumber", SqlDbType.NVarChar, user.MobileNumber.ToDbParameterValue());

                    if (!isOe3)
                    {
                        optionalCommands += ", birthday = @birthday";
                        cmd.AddParameter("birthday", SqlDbType.DateTime, user.Birthday.ToDbParameterValue());
                    }

                    if (isOe2)
                    {
                        optionalCommands += ", firstName = @firstName";
                        cmd.AddParameter("firstName", SqlDbType.NVarChar, user.FirstName.ToDbParameterValue());

                        optionalCommands += ", familyName = @familyName";
                        cmd.AddParameter("familyName", SqlDbType.NVarChar, user.FamilyName.ToDbParameterValue());
                    }

                    cmd.CommandText = "UPDATE ApplicationUser " +
                                      "SET organization = @organization, mobileNumber = @mobileNumber, street = @street, streetAttachment = @streetAttachment, zipCode = @zipCode, town = @town," +
                                      " countryCode = @countryCode, phoneNumber = @phoneNumber, skypeName = @skypeName," +
                                      " modifiedOn = @modifiedOn, emailAddress = @emailAddress, language = @language, modifiedBy = (select emailAddress from ApplicationUser where ID = @Id)" +
                                      optionalCommands +
                                      " WHERE ID = @Id";

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateUser(User user, string modifiedByUserId)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrEmpty(user.Id))
            {
                throw new ArgumentNullException(nameof(user.Id));
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    string commandAddition;
                    var commandBase = "UPDATE ApplicationUser " +
                                      "SET modifiedOn = @p2, modifiedBy = {0}, " +
                                      "familyName = @p4, firstName = @p5, " +
                                      "organization = @p6, street = @p7, streetAttachment = @p8, zipCode = @p9, town = @p10, countryCode = @p11, phoneNumber = @p12, " +
                                      "mobileNumber = @p13, birthday = @p14, emailAddress = @p15, fabasoftDossier = @p16, language = @p17, downloadLimitDisabledUntil = @p18, " +
                                      "RolePublicClient = @p19, ResearcherGroup = @p20, BarInternalConsultation = @p21, DigitalisierungsbeschraenkungAufgehobenBis = @p22 " +
                                      "WHERE ID = @p1";

                    if (string.IsNullOrEmpty(modifiedByUserId))
                    {
                        commandAddition = "'System'";
                    }
                    else
                    {
                        commandAddition = "(SELECT emailAddress FROM ApplicationUser WHERE ID = @p3)";

                        cmd.Parameters.Add(new SqlParameter
                        {
                            Value = modifiedByUserId,
                            ParameterName = "p3",
                            SqlDbType = SqlDbType.NVarChar
                        });
                    }

                    cmd.CommandText = string.Format(commandBase, commandAddition);

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.Id,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = DateTime.Now,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.FamilyName.ToDbParameterValue(),
                        ParameterName = "p4",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.FirstName.ToDbParameterValue(),
                        ParameterName = "p5",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.Organization.ToDbParameterValue(),
                        ParameterName = "p6",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.Street.ToDbParameterValue(),
                        ParameterName = "p7",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.StreetAttachment.ToDbParameterValue(),
                        ParameterName = "p8",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.ZipCode.ToDbParameterValue(),
                        ParameterName = "p9",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.Town.ToDbParameterValue(),
                        ParameterName = "p10",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.CountryCode.ToDbParameterValue(),
                        ParameterName = "p11",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.PhoneNumber.ToDbParameterValue(),
                        ParameterName = "p12",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.MobileNumber.ToDbParameterValue(),
                        ParameterName = "p13",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.Birthday.ToDbParameterValue(),
                        ParameterName = "p14",
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.EmailAddress.ToDbParameterValue(),
                        ParameterName = "p15",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.FabasoftDossier.ToDbParameterValue(),
                        ParameterName = "p16",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.Language.ToDbParameterValue(),
                        ParameterName = "p17",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.DownloadLimitDisabledUntil.ToDbParameterValue(),
                        ParameterName = "p18",
                        SqlDbType = SqlDbType.DateTime
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.RolePublicClient.ToDbParameterValue(),
                        ParameterName = "p19",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.ResearcherGroup,
                        ParameterName = "p20",
                        SqlDbType = SqlDbType.Bit
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.BarInternalConsultation,
                        ParameterName = "p21",
                        SqlDbType = SqlDbType.Bit
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = user.DigitalisierungsbeschraenkungAufgehobenBis.ToDbParameterValue(),
                        ParameterName = "p22",
                        SqlDbType = SqlDbType.DateTime
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateActiveSessionId(string userId, string sessionId)
        {
            if (userId == null) return;

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE ApplicationUser " +
                                      "SET ActiveAspNetSessionId = @p2, LastLoginDate = getdate() " +
                                      "WHERE ID = @p1";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = sessionId.ToDbParameterValue(),
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ClearActiveUserSessionIds()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE ApplicationUser " +
                                      "SET ActiveAspNetSessionId = @p1";
                    
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = DBNull.Value,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateUserSetting(JObject setting, string userId)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE ApplicationUser " +
                                      "SET Setting = @p2 " +
                                      "WHERE ID = @p1";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });


                    var settingString = setting != null ? JsonConvert.SerializeObject(setting, Formatting.Indented) : "";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = settingString.ToDbParameterValue(),
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.NVarChar
                    });


                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteAllAblieferdeStelleFromUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("DELETE FROM ApplicationUserAblieferndeStelle ");
                    query.Append("WHERE UserId = @p1 ");

                    cmd.CommandText += query;

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });


                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteAblieferdeStelleFromUser(string userId, int ablieferndeStelleId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            if (ablieferndeStelleId == 0)
            {
                return;
            }

            // Referenz zu einem Amt anfügen
            var query = new StringBuilder();
            query.Append("DELETE FROM ApplicationUserAblieferndeStelle ");
            query.Append("WHERE UserId = @p1 AND AblieferndeStelleId = @p2 ");

            AddOrDeleteAmtToUser(userId, ablieferndeStelleId, query.ToString());
        }

        public void CleanAndAddAblieferndeStelleToUser(string userId, List<int> ablieferndeStelleIds, string modifiedByUserId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            if (ablieferndeStelleIds == null)
            {
                return;
            }

            ablieferndeStelleIds = ablieferndeStelleIds.Count > 0
                ? ablieferndeStelleIds.Distinct().ToList()
                : new List<int>();

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append($"DELETE FROM ApplicationUserAblieferndeStelle WHERE UserId = '{userId}' ");

                    foreach (var ablieferndeStelleId in ablieferndeStelleIds)
                    {
                        query.Append($"IF EXISTS ( SELECT 1 FROM ApplicationUser WHERE Id = '{userId}') ");
                        query.Append($"AND EXISTS ( SELECT 1 FROM AblieferndeStelle WHERE AblieferndeStelleId = {ablieferndeStelleId}) ");
                        query.Append("BEGIN ");
                        query.Append(
                            $"  INSERT INTO ApplicationUserAblieferndeStelle (UserId,  AblieferndeStelleId) VALUES ('{userId}', {ablieferndeStelleId}) ");
                        query.Append("END ");
                    }

                    cmd.CommandText += query;
                    cmd.AddModifiedDataToCommand(userId, modifiedByUserId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public byte[] GetIdentifierDocument(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            byte[] identifierDocument = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT IdentifierDocument FROM ApplicationUser WHERE ID = @p1";
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            identifierDocument = reader["IdentifierDocument"] as byte[];
                        }
                    }
                }
            }

            return identifierDocument;
        }

        public void SetIdentifierDocument(string userId, byte[] file, string rolePublicClient)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var queryBuilder = new StringBuilder();
                    queryBuilder.Append("UPDATE ApplicationUser ");
                    queryBuilder.Append("SET IdentifierDocument = @p2, RolePublicClient = @p3 ");
                    queryBuilder.Append("WHERE ID = @p1");

                    cmd.CommandText = queryBuilder.ToString();
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = file.ToDbParameterValue(),
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.Binary
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = rolePublicClient.ToDbParameterValue(),
                        ParameterName = "p3",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public string GetRoleForClient(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return string.Empty;
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("SELECT apu.RolePublicClient ");
                    query.Append("FROM ApplicationUser apu ");
                    query.Append("WHERE apu.ID = @p1 ");
                    cmd.CommandText = query.ToString();

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId.ToDbParameterValue(),
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return reader["RolePublicClient"].ToString();
                        }
                    }
                }
            }

            return null;
        }

        public string GetEiamRoles(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return string.Empty;
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var query = new StringBuilder();
                    query.Append("SELECT apu.EiamRoles ");
                    query.Append("FROM ApplicationUser apu ");
                    query.Append("WHERE apu.ID = @p1 ");
                    cmd.CommandText = query.ToString();

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId.ToDbParameterValue(),
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return reader["EiamRoles"].ToString();
                        }
                    }
                }
            }

            return null;
        }

        public void StoreDownloadReasonInHistory(ElasticArchiveRecord record, User user, UserAccess access, int reasonId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    var bestandItem = record.ArchiveplanContext.FirstOrDefault(p => p.Level == "Bestand");
                    var bestandString = bestandItem == null ? string.Empty : bestandItem.RefCode + " " + bestandItem.Title;
                    var teilbestandItem = record.ArchiveplanContext.FirstOrDefault(p => p.Level == "Teilbestand");
                    var teilbestandString = teilbestandItem == null ? string.Empty : teilbestandItem.RefCode + " " + teilbestandItem.Title;
                    string zugaenglichkeitGemaessBga = record.HasCustomProperty("zugänglichkeitGemässBga")
                        ? record.CustomFields.zugänglichkeitGemässBga
                        : string.Empty;
                    var asTokens = string.Join(", ", access.AsTokens);
                    var ablieferndeStellen = string.Join(", ", user.AblieferndeStelleList.Select(a => a.Kuerzel));

                    cmd.CommandText =
                        "INSERT INTO DownloadReasonHistory (UserId, DownloadedAt, ReasonId, VeId, " +
                        "Signatur, Dossiertitel, Aktenzeichen, Entstehungszeitraum, Bestand, Teilbestand, Ablieferung, ZustaendigeStelleVe, " +
                        "Schutzfristverzeichnung, ZugaenglichkeitGemaessBga, " +
                        "FirstName, FamilyName, Organization, EmailAddress, RolePublicClient, AsAccessTokensUser, ZustaendigeStellenUser) " +
                        "VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17, @p18, @p19, @p20, @p21)";

                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p1", Value = access.UserId, SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p2", Value = DateTime.Now, SqlDbType = SqlDbType.DateTime});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p3", Value = reasonId, SqlDbType = SqlDbType.Int});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p4", Value = ToDb(record.ArchiveRecordId), SqlDbType = SqlDbType.Int});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p5", Value = ToDb(record.ReferenceCode), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p6", Value = ToDb(record.Title), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p7", Value = ToDb(record.Aktenzeichen()), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter
                        {ParameterName = "p8", Value = ToDb(record.CreationPeriod.Text), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p9", Value = ToDb(bestandString), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p10", Value = ToDb(teilbestandString), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p11", Value = ToDb(record.Ablieferung()), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter
                        {ParameterName = "p12", Value = ToDb(record.ZuständigeStelle()), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter
                        {ParameterName = "p13", Value = ToDb(record.GetSchutzfristenVerzeichnung()), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter
                        {ParameterName = "p14", Value = ToDb(zugaenglichkeitGemaessBga), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p15", Value = ToDb(user.FirstName), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p16", Value = ToDb(user.FamilyName), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p17", Value = ToDb(user.Organization), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p18", Value = ToDb(user.EmailAddress), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p19", Value = ToDb(user.RolePublicClient), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p20", Value = ToDb(asTokens), SqlDbType = SqlDbType.NVarChar});
                    cmd.Parameters.Add(new SqlParameter {ParameterName = "p21", Value = ToDb(ablieferndeStellen), SqlDbType = SqlDbType.NVarChar});

                    cmd.ExecuteScalar();
                }
            }
        }

        public void UpdateRejectionReason(string rejectionReason, string userId)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE ApplicationUser " +
                                      "SET ReasonForRejection = @p2, ReasonForRejectionDate = @p3, ModifiedOn = @p4, ModifiedBy = 'System' " +
                                      "WHERE ID = @p1";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = ToDb(rejectionReason),
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    object date;

                    if (!string.IsNullOrEmpty(rejectionReason))
                    {
                        date = DateTime.Now;
                    }
                    else
                    {
                        date = DBNull.Value;
                    }

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = date,
                        ParameterName = "p3",
                        SqlDbType = SqlDbType.DateTime
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = DateTime.Now,
                        ParameterName = "p4",
                        SqlDbType = SqlDbType.DateTime
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteOldRejectionReasons()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE ApplicationUser " +
                                      "SET ReasonForRejection = null, ReasonForRejectionDate = null,  ModifiedOn = @p2, ModifiedBy = 'System' " +
                                      "WHERE ReasonForRejectionDate IS NOT NULL AND ReasonForRejectionDate < @p1";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = DateTime.Now.AddMonths(-3),
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.DateTime
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = DateTime.Now,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.DateTime
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AddOrDeleteAmtToUser(string userId, int ablieferndeStelleId, string query)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            if (ablieferndeStelleId == 0)
            {
                return;
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText += query;

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = userId,
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });
                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = ablieferndeStelleId,
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.Int
                    });

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int TestDbAccess()
        {
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    cn.Open();
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = "select * from Version;";
                        var dbVersion = Convert.ToInt32(cmd.ExecuteScalar().ToString());
                        if (dbVersion > 0)
                        {
                            return dbVersion;
                        }
                    }
                }
            }
            catch
            {
                return -1;
            }

            return 0;
        }

        public List<User> GetUsersByName(string search)
        {
            var retVal = new List<User>();

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText =
                        $"{Sql} WHERE FirstName + ' ' + FamilyName like @p1 or FamilyName + ' ' + FirstName like @p2";

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = $"%{search}%",
                        ParameterName = "p1",
                        SqlDbType = SqlDbType.NVarChar
                    });

                    cmd.Parameters.Add(new SqlParameter
                    {
                        Value = $"%{search}%",
                        ParameterName = "p2",
                        SqlDbType = SqlDbType.NVarChar
                    });


                    using (var reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            retVal.Add(reader.ToUser<User>());
                        }
                    }
                }
            }

            return retVal;

        }
    }
}
