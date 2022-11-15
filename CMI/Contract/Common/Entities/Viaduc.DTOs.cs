﻿//------------------------------------------------------------------------------
// This is auto-generated code.
//------------------------------------------------------------------------------
// This code was generated by Devart Entity Developer tool using Data Transfer Object template.
// CM Informatik AG
//
// Changes to this file may cause incorrect behavior and will be lost if
// the code is regenerated.
//------------------------------------------------------------------------------

using System.Collections.Generic;

namespace CMI.Contract.Common.Entities
{

    public partial class CollectionDto
    {
        #region Constructors

        public CollectionDto() {
        }

        public CollectionDto(int collectionId, int? parentId, string language, string title, string descriptionShort, string description, global::System.DateTime validFrom, global::System.DateTime validTo, int collectionTypeId, byte[] image, byte[] thumbnail, string imageAltText, string imageMimeType, string link, string collectionPath, int sortOrder, global::System.DateTime createdOn, string createdBy, global::System.DateTime? modifiedOn, string modifiedBy, List<CollectionDto> childCollections, CollectionDto parent) {

          this.CollectionId = collectionId;
          this.ParentId = parentId;
          this.Language = language;
          this.Title = title;
          this.DescriptionShort = descriptionShort;
          this.Description = description;
          this.ValidFrom = validFrom;
          this.ValidTo = validTo;
          this.CollectionTypeId = collectionTypeId;
          this.Image = image;
          this.Thumbnail = thumbnail;
          this.ImageAltText = imageAltText;
          this.ImageMimeType = imageMimeType;
          this.Link = link;
          this.CollectionPath = collectionPath;
          this.SortOrder = sortOrder;
          this.CreatedOn = createdOn;
          this.CreatedBy = createdBy;
          this.ModifiedOn = modifiedOn;
          this.ModifiedBy = modifiedBy;
          this.ChildCollections = childCollections;
          this.Parent = parent;
        }

        #endregion

        #region Properties

        public int CollectionId { get; set; }

        public int? ParentId { get; set; }

        public string Language { get; set; }

        public string Title { get; set; }

        public string DescriptionShort { get; set; }

        public string Description { get; set; }

        public global::System.DateTime ValidFrom { get; set; }

        public global::System.DateTime ValidTo { get; set; }

        public int CollectionTypeId { get; set; }

        public byte[] Image { get; set; }

        public byte[] Thumbnail { get; set; }

        public string ImageAltText { get; set; }

        public string ImageMimeType { get; set; }

        public string Link { get; set; }

        public string CollectionPath { get; set; }

        public int SortOrder { get; set; }

        public global::System.DateTime CreatedOn { get; set; }

        public string CreatedBy { get; set; }

        public global::System.DateTime? ModifiedOn { get; set; }

        public string ModifiedBy { get; set; }

        #endregion

        #region Navigation Properties

        public List<CollectionDto> ChildCollections { get; set; }

        public CollectionDto Parent { get; set; }

        #endregion
    }

    public partial class CollectionListItemDto
    {
        #region Constructors

        public CollectionListItemDto() {
        }

        public CollectionListItemDto(int collectionId, int? parentId, string language, string title, string descriptionShort, string description, global::System.DateTime validFrom, global::System.DateTime validTo, int collectionTypeId, string imageAltText, string imageMimeType, string link, string collectionPath, int sortOrder, global::System.DateTime createdOn, string createdBy, global::System.DateTime? modifiedOn, string modifiedBy, string parent, string childCollections) {

          this.CollectionId = collectionId;
          this.ParentId = parentId;
          this.Language = language;
          this.Title = title;
          this.DescriptionShort = descriptionShort;
          this.Description = description;
          this.ValidFrom = validFrom;
          this.ValidTo = validTo;
          this.CollectionTypeId = collectionTypeId;
          this.ImageAltText = imageAltText;
          this.ImageMimeType = imageMimeType;
          this.Link = link;
          this.CollectionPath = collectionPath;
          this.SortOrder = sortOrder;
          this.CreatedOn = createdOn;
          this.CreatedBy = createdBy;
          this.ModifiedOn = modifiedOn;
          this.ModifiedBy = modifiedBy;
          this.Parent = parent;
          this.ChildCollections = childCollections;
        }

        #endregion

        #region Properties

        public int CollectionId { get; set; }

        public int? ParentId { get; set; }

        public string Language { get; set; }

        public string Title { get; set; }

        public string DescriptionShort { get; set; }

        public string Description { get; set; }

        public global::System.DateTime ValidFrom { get; set; }

        public global::System.DateTime ValidTo { get; set; }

        public int CollectionTypeId { get; set; }

        public string ImageAltText { get; set; }

        public string ImageMimeType { get; set; }

        public string Link { get; set; }

        public string CollectionPath { get; set; }

        public int SortOrder { get; set; }

        public global::System.DateTime CreatedOn { get; set; }

        public string CreatedBy { get; set; }

        public global::System.DateTime? ModifiedOn { get; set; }

        public string ModifiedBy { get; set; }

        public string Parent { get; set; }

        public string ChildCollections { get; set; }

        #endregion
    }

    public partial class ApplicationUserDto
    {
        #region Constructors

        public ApplicationUserDto() {
        }

        public ApplicationUserDto(string iD, string familyName, string firstName, string organization, string street, string streetAttachment, string zipCode, string town, string countryCode, string emailAddress, string phoneNumber, string skypeName, string setting, string claims, global::System.DateTime created, global::System.DateTime updated, string fulltext, string userExtId, string language, global::System.DateTime createdOn, string createdBy, global::System.DateTime modifiedOn, string modifiedBy, global::System.DateTime? birthday, string fabasoftDossier, string reasonForRejection, bool isInternalUser, string rolePublicClient, string eiamRoles, bool researcherGroup, bool barInternalConsultation, byte[] identifierDocument, string mobileNumber, global::System.DateTime? reasonForRejectionDate, global::System.DateTime? downloadLimitDisabledUntil, global::System.DateTime? digitalisierungsbeschraenkungAufgehobenBis, string activeAspNetSessionId) {

          this.ID = iD;
          this.FamilyName = familyName;
          this.FirstName = firstName;
          this.Organization = organization;
          this.Street = street;
          this.StreetAttachment = streetAttachment;
          this.ZipCode = zipCode;
          this.Town = town;
          this.CountryCode = countryCode;
          this.EmailAddress = emailAddress;
          this.PhoneNumber = phoneNumber;
          this.SkypeName = skypeName;
          this.Setting = setting;
          this.Claims = claims;
          this.Created = created;
          this.Updated = updated;
          this.Fulltext = fulltext;
          this.UserExtId = userExtId;
          this.Language = language;
          this.CreatedOn = createdOn;
          this.CreatedBy = createdBy;
          this.ModifiedOn = modifiedOn;
          this.ModifiedBy = modifiedBy;
          this.Birthday = birthday;
          this.FabasoftDossier = fabasoftDossier;
          this.ReasonForRejection = reasonForRejection;
          this.IsInternalUser = isInternalUser;
          this.RolePublicClient = rolePublicClient;
          this.EiamRoles = eiamRoles;
          this.ResearcherGroup = researcherGroup;
          this.BarInternalConsultation = barInternalConsultation;
          this.IdentifierDocument = identifierDocument;
          this.MobileNumber = mobileNumber;
          this.ReasonForRejectionDate = reasonForRejectionDate;
          this.DownloadLimitDisabledUntil = downloadLimitDisabledUntil;
          this.DigitalisierungsbeschraenkungAufgehobenBis = digitalisierungsbeschraenkungAufgehobenBis;
          this.ActiveAspNetSessionId = activeAspNetSessionId;
        }

        #endregion

        #region Properties

        public string ID { get; set; }

        public string FamilyName { get; set; }

        public string FirstName { get; set; }

        public string Organization { get; set; }

        public string Street { get; set; }

        public string StreetAttachment { get; set; }

        public string ZipCode { get; set; }

        public string Town { get; set; }

        public string CountryCode { get; set; }

        public string EmailAddress { get; set; }

        public string PhoneNumber { get; set; }

        public string SkypeName { get; set; }

        public string Setting { get; set; }

        public string Claims { get; set; }

        public global::System.DateTime Created { get; set; }

        public global::System.DateTime Updated { get; set; }

        public string Fulltext { get; set; }

        public string UserExtId { get; set; }

        public string Language { get; set; }

        public global::System.DateTime CreatedOn { get; set; }

        public string CreatedBy { get; set; }

        public global::System.DateTime ModifiedOn { get; set; }

        public string ModifiedBy { get; set; }

        public global::System.DateTime? Birthday { get; set; }

        public string FabasoftDossier { get; set; }

        public string ReasonForRejection { get; set; }

        public bool IsInternalUser { get; set; }

        public string RolePublicClient { get; set; }

        public string EiamRoles { get; set; }

        public bool ResearcherGroup { get; set; }

        public bool BarInternalConsultation { get; set; }

        public byte[] IdentifierDocument { get; set; }

        public string MobileNumber { get; set; }

        public global::System.DateTime? ReasonForRejectionDate { get; set; }

        public global::System.DateTime? DownloadLimitDisabledUntil { get; set; }

        public global::System.DateTime? DigitalisierungsbeschraenkungAufgehobenBis { get; set; }

        public string ActiveAspNetSessionId { get; set; }

        #endregion
    }

    public partial class ManuelleKorrekturDto
    {
        #region Constructors

        public ManuelleKorrekturDto() {
        }

        public ManuelleKorrekturDto(int manuelleKorrekturId, int veId, string signatur, global::System.DateTime schutzfristende, string titel, global::System.DateTime erzeugtAm, string erzeugtVon, global::System.DateTime? geändertAm, string geändertVon, int anonymisierungsstatus, string kommentar, string hierachiestufe, string aktenzeichen, string entstehungszeitraum, string zugänglichkeitGemässBGA, string schutzfristverzeichnung, string zuständigeStelle, string publikationsrechte, bool anonymisiertZumErfassungszeitpunk, List<ManuelleKorrekturFeldDto> manuelleKorrekturFelder, List<ManuelleKorrekturStatusHistoryDto> manuelleKorrekturStatusHistories) {

          this.ManuelleKorrekturId = manuelleKorrekturId;
          this.VeId = veId;
          this.Signatur = signatur;
          this.Schutzfristende = schutzfristende;
          this.Titel = titel;
          this.ErzeugtAm = erzeugtAm;
          this.ErzeugtVon = erzeugtVon;
          this.GeändertAm = geändertAm;
          this.GeändertVon = geändertVon;
          this.Anonymisierungsstatus = anonymisierungsstatus;
          this.Kommentar = kommentar;
          this.Hierachiestufe = hierachiestufe;
          this.Aktenzeichen = aktenzeichen;
          this.Entstehungszeitraum = entstehungszeitraum;
          this.ZugänglichkeitGemässBGA = zugänglichkeitGemässBGA;
          this.Schutzfristverzeichnung = schutzfristverzeichnung;
          this.ZuständigeStelle = zuständigeStelle;
          this.Publikationsrechte = publikationsrechte;
          this.AnonymisiertZumErfassungszeitpunk = anonymisiertZumErfassungszeitpunk;
          this.ManuelleKorrekturFelder = manuelleKorrekturFelder;
          this.ManuelleKorrekturStatusHistories = manuelleKorrekturStatusHistories;
        }

        #endregion

        #region Properties

        public int ManuelleKorrekturId { get; set; }

        public int VeId { get; set; }

        public string Signatur { get; set; }

        public global::System.DateTime Schutzfristende { get; set; }

        public string Titel { get; set; }

        public global::System.DateTime ErzeugtAm { get; set; }

        public string ErzeugtVon { get; set; }

        public global::System.DateTime? GeändertAm { get; set; }

        public string GeändertVon { get; set; }

        public int Anonymisierungsstatus { get; set; }

        public string Kommentar { get; set; }

        public string Hierachiestufe { get; set; }

        public string Aktenzeichen { get; set; }

        public string Entstehungszeitraum { get; set; }

        public string ZugänglichkeitGemässBGA { get; set; }

        public string Schutzfristverzeichnung { get; set; }

        public string ZuständigeStelle { get; set; }

        public string Publikationsrechte { get; set; }

        public bool AnonymisiertZumErfassungszeitpunk { get; set; }

        #endregion

        #region Navigation Properties

        public List<ManuelleKorrekturFeldDto> ManuelleKorrekturFelder { get; set; }

        public List<ManuelleKorrekturStatusHistoryDto> ManuelleKorrekturStatusHistories { get; set; }

        #endregion
    }

    public partial class ManuelleKorrekturFeldDto
    {
        #region Constructors

        public ManuelleKorrekturFeldDto() {
        }

        public ManuelleKorrekturFeldDto(int manuelleKorrekturFelderId, int manuelleKorrekturId, string feldname, string original, string automatisch, string manuell, ManuelleKorrekturDto manuelleKorrektur) {

          this.ManuelleKorrekturFelderId = manuelleKorrekturFelderId;
          this.ManuelleKorrekturId = manuelleKorrekturId;
          this.Feldname = feldname;
          this.Original = original;
          this.Automatisch = automatisch;
          this.Manuell = manuell;
          this.ManuelleKorrektur = manuelleKorrektur;
        }

        #endregion

        #region Properties

        public int ManuelleKorrekturFelderId { get; set; }

        public int ManuelleKorrekturId { get; set; }

        public string Feldname { get; set; }

        public string Original { get; set; }

        public string Automatisch { get; set; }

        public string Manuell { get; set; }

        #endregion

        #region Navigation Properties

        public ManuelleKorrekturDto ManuelleKorrektur { get; set; }

        #endregion
    }

    public partial class ManuelleKorrekturStatusHistoryDto
    {
        #region Constructors

        public ManuelleKorrekturStatusHistoryDto() {
        }

        public ManuelleKorrekturStatusHistoryDto(int manuelleKorrekturStatusHistoryId, int manuelleKorrekturId, int anonymisierungsstatus, global::System.DateTime erzeugtAm, string erzeugtVon, ManuelleKorrekturDto manuelleKorrektur) {

          this.ManuelleKorrekturStatusHistoryId = manuelleKorrekturStatusHistoryId;
          this.ManuelleKorrekturId = manuelleKorrekturId;
          this.Anonymisierungsstatus = anonymisierungsstatus;
          this.ErzeugtAm = erzeugtAm;
          this.ErzeugtVon = erzeugtVon;
          this.ManuelleKorrektur = manuelleKorrektur;
        }

        #endregion

        #region Properties

        public int ManuelleKorrekturStatusHistoryId { get; set; }

        public int ManuelleKorrekturId { get; set; }

        public int Anonymisierungsstatus { get; set; }

        public global::System.DateTime ErzeugtAm { get; set; }

        public string ErzeugtVon { get; set; }

        #endregion

        #region Navigation Properties

        public ManuelleKorrekturDto ManuelleKorrektur { get; set; }

        #endregion
    }

    public partial class VManuelleKorrekturDto
    {
        #region Constructors

        public VManuelleKorrekturDto() {
        }

        public VManuelleKorrekturDto(int manuelleKorrekturId, int veId, string signatur, global::System.DateTime schutzfristende, string titel, global::System.DateTime erzeugtAm, string erzeugtVon, global::System.DateTime? geändertAm, string geändertVon, int anonymisierungsstatus, string kommentar, string hierachiestufe, string aktenzeichen, string entstehungszeitraum, string zugänglichkeitGemässBGA, string schutzfristverzeichnung, string zuständigeStelle, string publikationsrechte, bool anonymisiertZumErfassungszeitpunk, string titelGemAIS, string titelAutomatischAnonymisiert, string titelManuellKorrigiert, string darinGemAIS, string darinAutomatischAnonymisiert, string darinManuellKorrigiert, string zusatzkomponenteGemAIS, string zusatzkomponenteAutomatischAnonymisiert, string zusatzkomponenteManuellKorrigiert, string zusaetzlicheInformationenGemAIS, string zusaetzlicheInformationenAutomatischAnonymisiert, string zusaetzlicheInformationenManuellKorrigiert, string verwandteVEGemAIS, string verwandteVEAutomatischAnonymisiert, string verwandteVEManuellKorrigiert) {

          this.ManuelleKorrekturId = manuelleKorrekturId;
          this.VeId = veId;
          this.Signatur = signatur;
          this.Schutzfristende = schutzfristende;
          this.Titel = titel;
          this.ErzeugtAm = erzeugtAm;
          this.ErzeugtVon = erzeugtVon;
          this.GeändertAm = geändertAm;
          this.GeändertVon = geändertVon;
          this.Anonymisierungsstatus = anonymisierungsstatus;
          this.Kommentar = kommentar;
          this.Hierachiestufe = hierachiestufe;
          this.Aktenzeichen = aktenzeichen;
          this.Entstehungszeitraum = entstehungszeitraum;
          this.ZugänglichkeitGemässBGA = zugänglichkeitGemässBGA;
          this.Schutzfristverzeichnung = schutzfristverzeichnung;
          this.ZuständigeStelle = zuständigeStelle;
          this.Publikationsrechte = publikationsrechte;
          this.AnonymisiertZumErfassungszeitpunk = anonymisiertZumErfassungszeitpunk;
          this.TitelGemAIS = titelGemAIS;
          this.TitelAutomatischAnonymisiert = titelAutomatischAnonymisiert;
          this.TitelManuellKorrigiert = titelManuellKorrigiert;
          this.DarinGemAIS = darinGemAIS;
          this.DarinAutomatischAnonymisiert = darinAutomatischAnonymisiert;
          this.DarinManuellKorrigiert = darinManuellKorrigiert;
          this.ZusatzkomponenteGemAIS = zusatzkomponenteGemAIS;
          this.ZusatzkomponenteAutomatischAnonymisiert = zusatzkomponenteAutomatischAnonymisiert;
          this.ZusatzkomponenteManuellKorrigiert = zusatzkomponenteManuellKorrigiert;
          this.ZusaetzlicheInformationenGemAIS = zusaetzlicheInformationenGemAIS;
          this.ZusaetzlicheInformationenAutomatischAnonymisiert = zusaetzlicheInformationenAutomatischAnonymisiert;
          this.ZusaetzlicheInformationenManuellKorrigiert = zusaetzlicheInformationenManuellKorrigiert;
          this.VerwandteVEGemAIS = verwandteVEGemAIS;
          this.VerwandteVEAutomatischAnonymisiert = verwandteVEAutomatischAnonymisiert;
          this.VerwandteVEManuellKorrigiert = verwandteVEManuellKorrigiert;
        }

        #endregion

        #region Properties

        public int ManuelleKorrekturId { get; set; }

        public int VeId { get; set; }

        public string Signatur { get; set; }

        public global::System.DateTime Schutzfristende { get; set; }

        public string Titel { get; set; }

        public global::System.DateTime ErzeugtAm { get; set; }

        public string ErzeugtVon { get; set; }

        public global::System.DateTime? GeändertAm { get; set; }

        public string GeändertVon { get; set; }

        public int Anonymisierungsstatus { get; set; }

        public string Kommentar { get; set; }

        public string Hierachiestufe { get; set; }

        public string Aktenzeichen { get; set; }

        public string Entstehungszeitraum { get; set; }

        public string ZugänglichkeitGemässBGA { get; set; }

        public string Schutzfristverzeichnung { get; set; }

        public string ZuständigeStelle { get; set; }

        public string Publikationsrechte { get; set; }

        public bool AnonymisiertZumErfassungszeitpunk { get; set; }

        public string TitelGemAIS { get; set; }

        public string TitelAutomatischAnonymisiert { get; set; }

        public string TitelManuellKorrigiert { get; set; }

        public string DarinGemAIS { get; set; }

        public string DarinAutomatischAnonymisiert { get; set; }

        public string DarinManuellKorrigiert { get; set; }

        public string ZusatzkomponenteGemAIS { get; set; }

        public string ZusatzkomponenteAutomatischAnonymisiert { get; set; }

        public string ZusatzkomponenteManuellKorrigiert { get; set; }

        public string ZusaetzlicheInformationenGemAIS { get; set; }

        public string ZusaetzlicheInformationenAutomatischAnonymisiert { get; set; }

        public string ZusaetzlicheInformationenManuellKorrigiert { get; set; }

        public string VerwandteVEGemAIS { get; set; }

        public string VerwandteVEAutomatischAnonymisiert { get; set; }

        public string VerwandteVEManuellKorrigiert { get; set; }

        #endregion
    }

}
