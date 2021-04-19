-- ---------------------------------------------------------------------------------------
-- Indexspezifische Daten bei der Bestellung Snapshoten für die Auftragsübersicht
-- ---------------------------------------------------------------------------------------
ALTER TABLE OrderItem
ADD 
Standort NVARCHAR(MAX),
Signatur NVARCHAR(MAX),
Darin NVARCHAR(MAX),
ZusaetzlicheInformationen NVARCHAR(MAX),
Hierarchiestufe NVARCHAR(MAX),
Schutzfristverzeichnung NVARCHAR(MAX),
ZugaenglichkeitGemaessBga NVARCHAR(MAX),
Publikationsrechte NVARCHAR(MAX),
Behaeltnistyp NVARCHAR(MAX),
ZustaendigeStelle NVARCHAR(MAX),
IdentifikationDigitalesMagazin NVARCHAR(MAX)

/* Bestehende Tabellen leeren, da neue Bestellungen nun Indexdaten enthalten */
TRUNCATE TABLE OrderItem
GO
DELETE FROM Ordering WHERE Ordering.ID NOT IN (SELECT OrderId FROM OrderItem);
GO

/* View für Auftragsübersicht mit den neuen Feldern ergänzen */
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
	i.Behaeltnistyp,
	i.ZustaendigeStelle,
	i.IdentifikationDigitalesMagazin,
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
GO
