using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using CMI.Access.Sql.Viaduc.AblieferndeStellen.Dto;
using CMI.Contract.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Access.Sql.Viaduc
{
    public class User
    {
        [EditNotAllowed] [RequiredDtoField] public string Id { get; set; }

        [EditNotAllowedFor(AccessRolesEnum.Ö3, AccessRolesEnum.AS, AccessRolesEnum.BAR, AccessRolesEnum.BVW)]
        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        [RequiredDtoField]
        public string FamilyName { get; set; }

        [EditNotAllowedFor(AccessRolesEnum.Ö3, AccessRolesEnum.AS, AccessRolesEnum.BAR, AccessRolesEnum.BVW)]
        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        [RequiredDtoField]
        public string FirstName { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        public string Organization { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        [RequiredDtoField]
        public string Street { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        public string StreetAttachment { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        [RequiredDtoField]
        public string ZipCode { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        [RequiredDtoField]
        public string Town { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        [RequiredDtoField]
        public string CountryCode { get; set; }

        [EditNotAllowedFor(AccessRolesEnum.AS, AccessRolesEnum.BAR, AccessRolesEnum.BVW)]
        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        [RequiredDtoField]
        public string EmailAddress { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        public string PhoneNumber { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        public string SkypeName { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        public string MobileNumber { get; set; }

        [EditNotAllowed] public string UserExtId { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        [RequiredDtoField]
        public string Language { get; set; }

        [EditNotAllowed] public DateTime CreatedOn { get; set; }

        [EditNotAllowed] public string CreatedBy { get; set; }

        [EditNotAllowed] public DateTime ModifiedOn { get; set; }

        [EditNotAllowed] public string ModifiedBy { get; set; }

        [EditNotAllowedFor(AccessRolesEnum.Ö3)]
        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        public DateTime? Birthday { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungBereichBenutzerdatenBearbeiten)]
        public string FabasoftDossier { get; set; }

        [EditNotAllowed] public string ReasonForRejection { get; set; }

        [EditNotAllowed] public bool IsInternalUser { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungFeldBenutzerrollePublicClientBearbeiten)]
        public string RolePublicClient { get; set; }

        [EditNotAllowed] public string EiamRoles { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungFeldForschungsgruppeDdsBearbeiten)]
        public bool ResearcherGroup { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungFeldBarInterneKonsultationBearbeiten)]
        public bool BarInternalConsultation { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungUploadAusfuehren)]
        public bool HasIdentifizierungsmittel { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungFeldDownloadbeschraenkungBearbeiten)]
        public DateTime? DownloadLimitDisabledUntil { get; set; }

        [EditRequiresFeature(ApplicationFeature.BenutzerUndRollenBenutzerverwaltungFeldDigitalisierungsbeschraenkungBearbeiten)]
        public DateTime? DigitalisierungsbeschraenkungAufgehobenBis { get; set; }

        [EditIgnore] [JsonIgnore] public JObject Setting { get; set; }

        [EditIgnore] [JsonIgnore] public JObject Claims { get; set; }
        [EditIgnore] [JsonIgnore] public string ActiveAspNetSessionId { get; set; }

        [EditNotAllowed] public List<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();

        [EditNotAllowed] public List<ApplicationFeature> Features { get; set; } = new List<ApplicationFeature>();

        [EditNotAllowed] public List<AblieferndeStelleDto> AblieferndeStelleList { get; set; } = new List<AblieferndeStelleDto>();

        [EditIgnore] public UserAccess Access { get; set; }

        public string CreateModifiyData
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Erstellt am:\t\t\t\t{CreatedOn:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"Erstellt von:\t\t\t\t{CreatedBy}");
                sb.AppendLine($"Letzte Änderung am:\t{ModifiedOn:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"Letzte Änderung von:\t{ModifiedBy}");
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return
                $"{FirstName} {FamilyName}{(!string.IsNullOrEmpty(Organization) ? "," : "")} {Organization}"
                    .Trim();
        }
    }

    public class ReadUserInformationRequest
    {
        public string UserId { get; set; }
    }

    public class ReadUserInformationResponse
    {
        public User User { get; set; }
    }

    public static class UserExtensions
    {
        public static T ToUser<T>(this SqlDataReader reader, string userId = null) where T : User, new()
        {
            var user = new T
            {
                Id = userId ?? reader["Id"] as string
            };

            reader.PopulateProperties(user);

            return user;
        }
    }
}