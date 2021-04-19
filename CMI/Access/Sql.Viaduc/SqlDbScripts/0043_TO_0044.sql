ALTER TABLE dbo.OrderItem ADD
	DatumDesEntscheids datetime NULL,
	Ausgabedatum datetime NULL,
	Abschlussdatum datetime NULL,
	Abbruchgrund int NOT NULL CONSTRAINT DF_OrderItem_Abbruchgrund DEFAULT 0, 
	EntscheidGesuch int NOT NULL CONSTRAINT DF_OrderItem_EntscheidGesuch DEFAULT 0
GO

ALTER TABLE dbo.OrderItem
ADD CONSTRAINT DF_OrderItem_DigitalisierungsKategorie DEFAULT 0 for DigitalisierungsKategorie

UPDATE OrderItem SET DigitalisierungsKategorie = 0 where DigitalisierungsKategorie IS NULL

ALTER TABLE dbo.OrderItem 
ALTER COLUMN DigitalisierungsKategorie int NOT NULL 


EXECUTE sp_addextendedproperty N'MS_Description', N'Datum, an welchem der Entscheid über ein Einsichtsgesuch getroffen wurde.', 'SCHEMA', N'dbo', 'TABLE', N'OrderItem', 'COLUMN', N'DatumDesEntscheids'
GO
EXECUTE sp_addextendedproperty N'MS_Description', N'Datum, an welchem eine Archiveinheit ausgeliehen wurde (dem Ausleiher ausgehändigt)', 'SCHEMA', N'dbo', 'TABLE', N'OrderItem', 'COLUMN', N'Ausgabedatum'
GO
EXECUTE sp_addextendedproperty N'MS_Description', N'Datum, an welchem der Auftrag abgeschlossen wurde.', 'SCHEMA', N'dbo', 'TABLE', N'OrderItem', 'COLUMN', N'Abschlussdatum'
GO
EXECUTE sp_addextendedproperty N'MS_Description', N'Numerischer Code des Entscheids, welcher über ein Einsichtsgesuch getroffen wurde.', 'SCHEMA', N'dbo', 'TABLE', N'OrderItem', 'COLUMN', N'EntscheidGesuch'
GO

CREATE NONCLUSTERED INDEX [IX_ForCalcIndividualAccessTokens] ON [dbo].[OrderItem]
(
	[Ve] ASC,
	[ApproveStatus] ASC,
	[EntscheidGesuch] ASC
)
INCLUDE ( 	[ID],
	[OrderId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

-- v_Auftragsuebersicht löschen und v_OrderingFlatItem erstellen
DROP VIEW v_OrderingFlatItem;
GO

CREATE VIEW v_OrderingFlatItem AS
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
	r.Name_de AS Reason,
	i.ZeitraumDossier AS ZeitraumDossier,
	i.EntscheidGesuch AS EntscheidGesuch,
	i.DatumDesEntscheids AS DatumDesEntscheids,
	i.Ausgabedatum AS Ausgabedatum,
	i.Abschlussdatum AS Abschlussdatum,
	i.Abbruchgrund AS Abbruchgrund,
	o.Id AS OrderId,
	u.Firstname + ' ' + u.FamilyName + IIF(u.Organization IS NOT NULL, ', ' + u.Organization, '') As [User],
	[o].[Type] AS OrderingType, 
	o.Comment AS OrderingComment, 
	o.UserId,
	o.LesesaalDate AS OrderingLesesaalDate, 
	a.Name_de AS OrderingArtDerArbeit, 
	o.ArtDerArbeit as OrderingArtDerArbeitId,
	o.orderDate AS OrderingDate,
	CASE
		WHEN [Status] = 0 THEN 0
		WHEN [Status] = 9 AND o.[Type] <> 2 THEN 2
		WHEN [Status] = 5 THEN 4
		WHEN [Status] = 12 OR [Status] = 13 THEN 3
		ELSE 1
    END AS ExternalStatus
FROM
	OrderItem i
	INNER JOIN Ordering o ON i.OrderId = o.ID
	INNER JOIN ApplicationUser u ON u.ID = o.UserId
	LEFT JOIN Reason r ON r.ID = i.Reason
	LEFT JOIN ArtDerArbeit a ON a.ID = o.ArtDerArbeit
WHERE
	o.orderDate  IS NOT NULL