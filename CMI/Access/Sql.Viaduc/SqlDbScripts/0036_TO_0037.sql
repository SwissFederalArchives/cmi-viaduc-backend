/* Recreate View mit neuen Spalten */
DROP VIEW v_Auftragsuebersicht;
GO
CREATE VIEW v_Auftragsuebersicht AS
	SELECT 
	i.Id AS ItemId,
	i.Ve AS VeId,
	i.Comment AS ItemComment,
	i.BewilligungsDatum AS BewilligungsDatum,
	i.Bestand AS Bestand,
	i.Ablieferung AS Ablieferung,
	i.BehaeltnisNummer AS BehaeltnisNummer,
	i.DossierTitel AS dossiertitel,
	i.HasPersonendaten AS HasPersonendaten,
	i.Standort as Standort,
	i.Signatur as Signatur,
	i.Darin as Darin,
	i.ZusaetzlicheInformationen,
	i.Hierarchiestufe,
	i.Schutzfristverzeichnung,
	i.ArchivNummer,
	i.ZugaenglichkeitGemaessBga,
	i.Publikationsrechte,
	i.[Status],
	i.Reason as ReasonId,
	i.Behaeltnistyp,
	i.ZustaendigeStelle,
	i.ApproveStatus,
	i.DigitalisierungsKategorie,
	i.TerminDigitalisierung,
	i.InternalComment,
	i.Aktenzeichen,
	i.IdentifikationDigitalesMagazin,
	(SELECT Name_de FROM Reason WHERE Reason.ID = i.Reason) AS Reason,
	i.ZeitraumDossier AS ZeitraumDossier,

	o.Id AS OrderId,
	(SELECT Firstname + ' ' + FamilyName + IIF(Organization IS NOT NULL, ', ' + Organization, '') FROM ApplicationUser WHERE ApplicationUser.ID = o.UserId) AS [User],
	[o].[Type] AS OrderingType, 
	o.Comment AS OrderingComment, 
	o.UserId,
	o.LesesaalDate AS OrderingLesesaalDate, 
	(SELECT Name_de FROM ArtDerArbeit WHERE ArtDerArbeit.ID = o.ArtDerArbeit) AS OrderingArtDerArbeit, 
	o.ArtDerArbeit as OrderingArtDerArbeitId,
	o.orderDate AS OrderingDate

FROM OrderItem i 
	LEFT JOIN Ordering o 
	ON i.OrderId = o.ID 
WHERE o.OrderDate IS NOT NULL AND o.[Type] <> 4;
GO
