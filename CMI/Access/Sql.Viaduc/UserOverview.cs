using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq.Mapping;

namespace CMI.Access.Sql.Viaduc
{
    [Table(Name = "v_UserOverview")]
    public class UserOverview
    {
        [Key]
        [Column(IsPrimaryKey = true, IsDbGenerated = false)]
        public string Id { get; set; }

        [Column(CanBeNull = true)] public string UserExtId { get; set; }

        [Column(CanBeNull = false)] public DateTime Created { get; set; }

        [Column(CanBeNull = false)] public DateTime CreatedOn { get; set; }

        [Column(CanBeNull = true)] public string CreatedBy { get; set; }

        [Column(CanBeNull = true)] public DateTime ModifiedOn { get; set; }

        [Column(CanBeNull = false)] public string ModifiedBy { get; set; }

        [Column(CanBeNull = true)] public string FamilyName { get; set; }

        [Column(CanBeNull = true)] public string FirstName { get; set; }

        [Column(CanBeNull = true)] public string Organization { get; set; }

        [Column(CanBeNull = true)] public string Street { get; set; }

        [Column(CanBeNull = true)] public string StreetAttachment { get; set; }

        [Column(CanBeNull = true)] public string ZipCode { get; set; }

        [Column(CanBeNull = true)] public string Town { get; set; }

        [Column(CanBeNull = true)] public string CountryCode { get; set; }

        [Column(CanBeNull = true)] public string PhoneNumber { get; set; }

        [Column(CanBeNull = true)] public string MobileNumber { get; set; }

        [Column(CanBeNull = true)] public DateTime? Birthday { get; set; }

        [Column(CanBeNull = true)] public string EmailAddress { get; set; }

        [Column(CanBeNull = true)] public string FabasoftDossier { get; set; }

        [Column(CanBeNull = false)] public string Language { get; set; }

        [Column(CanBeNull = true)] public string RolePublicClient { get; set; }

        [Column(CanBeNull = true)] public string EiamRoles { get; set; }

        [Column(CanBeNull = true)] public string ReasonForRejection { get; set; }

        [Column(CanBeNull = false)] public bool ResearcherGroup { get; set; }

        [Column(CanBeNull = false)] public bool BarInternalConsultation { get; set; }

        [Column(CanBeNull = false)] public bool IsIdentifiedUser { get; set; }

        [Column(CanBeNull = false)] public int QoAValue { get; set; }

        [Column(CanBeNull = false)] public string HomeName { get; set; }

        [Column(CanBeNull = true)] public string AblieferndeStellenId { get; set; }

        [Column(CanBeNull = true)] public string AblieferndeStellenKuerzel { get; set; }

        [Column(CanBeNull = true)] public string ApplicationUserRollesId { get; set; }

        [Column(CanBeNull = true)] public string ApplicationUserRolles { get; set; }

        [Column(CanBeNull = true)] public string AblieferndeStellenTokenId { get; set; }

        [Column(CanBeNull = true)] public string AblieferndeStellenToken { get; set; }

        [Column(CanBeNull = false)] public bool Identifizierungsmittel { get; set; }

        [Column(CanBeNull = true)] public DateTime? DownloadLimitDisabledUntil { get; set; }

        [Column(CanBeNull = true)] public DateTime? DigitalisierungsbeschraenkungAufgehobenBis { get; set; }

        [Column(CanBeNull = true)] public DateTime? LastLoginDate { get; set; }
    }
}