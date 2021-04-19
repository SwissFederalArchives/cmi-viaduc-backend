/* KEEP IN SYNC WITH OrderingFlatItems.cs  */

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
	(SELECT Name_de FROM Reason WHERE Reason.ID = i.Reason) AS Reason,
	i.ZeitraumDossier AS ZeitraumDossier,

	o.Id AS OrderId,
	(SELECT Firstname + ' ' + FamilyName + IIF(Organization IS NOT NULL, ', ' + Organization, '') FROM ApplicationUser WHERE ApplicationUser.ID = o.UserId) AS [User],
	[o].[Type] AS OrderingType, 
	o.Comment AS OrderingComment, 
	o.LesesaalDate AS OrderingLesesaalDate, 
	(SELECT Name_de FROM ArtDerArbeit WHERE ArtDerArbeit.ID = o.ArtDerArbeit) AS OrderingArtDerArbeit, 
	o.orderDate AS OrderingDate

FROM OrderItem i 
	LEFT JOIN Ordering o 
	ON i.OrderId = o.ID 
WHERE o.OrderDate IS NOT NULL AND o.[Type] <> 4;
