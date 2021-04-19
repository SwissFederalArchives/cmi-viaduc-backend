using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using CMI.Contract.Common;

namespace CMI.Access.Sql.Viaduc
{
    public class UserAccess
    {
        public UserAccess(string userId, string rolePublicClient, string eiamRole, string[] asTokens, bool researcherGroup, string language = null)
        {
            UserId = userId;
            ResearcherGroup = researcherGroup;
            AsTokens = asTokens ?? new string [0];
            RolePublicClient = !string.IsNullOrWhiteSpace(rolePublicClient) ? rolePublicClient : AccessRoles.RoleOe1;
            EiamRole = !string.IsNullOrWhiteSpace(eiamRole) ? eiamRole : string.Empty;
            Language = language ?? "de";
        }

        [Obsolete("Only for XML Serialization !")]
        public UserAccess()
        {
        }

        public string UserId { get; set; }
        public string RolePublicClient { get; set; }
        public string[] AsTokens { get; set; }
        public string EiamRole { get; set; }

        [XmlIgnore]
        public string[] CombinedTokens
        {
            get
            {
                var combinedTokens = new List<string>();

                combinedTokens.Add(RolePublicClient);

                switch (RolePublicClient.GetRolePublicClientEnum())
                {
                    case AccessRolesEnum.Ö1:
                        break;

                    case AccessRolesEnum.Ö2:
                        combinedTokens.Add($"FG_{UserId}");
                        break;

                    case AccessRolesEnum.Ö3:
                        if (ResearcherGroup)
                        {
                            combinedTokens.Add("DDS");
                        }
                        else
                        {
                            combinedTokens.Add($"FG_{UserId}");
                            combinedTokens.Add($"EB_{UserId}");
                        }

                        break;

                    case AccessRolesEnum.BVW:
                        combinedTokens.Add($"FG_{UserId}");
                        combinedTokens.Add($"EB_{UserId}");
                        break;

                    case AccessRolesEnum.AS:
                        combinedTokens.Add($"FG_{UserId}");
                        combinedTokens.Add($"EB_{UserId}");
                        combinedTokens.AddRange(AsTokens ?? new string[0]);
                        break;

                    case AccessRolesEnum.BAR:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return combinedTokens.ToArray();
            }
        }

        public bool ResearcherGroup { get; set; }

        public string Language { get; set; }

        public bool HasAnyTokenFor(IEnumerable<string> tokens)
        {
            return tokens != null && CombinedTokens.Intersect(tokens).Any();
        }

        public bool HasNonIndividualTokenFor(IEnumerable<string> tokens)
        {
            return CombinedTokens
                .Where(t => !t.StartsWith("FG_"))
                .Where(t => !t.StartsWith("EB_"))
                .Intersect(tokens).Any();
        }

        public bool HasAsTokenFor(IEnumerable<string> tokens)
        {
            return AsTokens?
                .Intersect(tokens).Any() ?? false;
        }
    }
}