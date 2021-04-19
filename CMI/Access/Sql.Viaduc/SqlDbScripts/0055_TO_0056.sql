/* Neue Spalten hinzufügen */
ALTER TABLE dbo.OrderItem ADD
	Benutzungskopie bit NULL CONSTRAINT DF_OrderItem_Benutzungskopie DEFAULT NULL
GO

EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'Benutzungskopie', @value=N'Gibt an, ob es sich hierbei um eine Benutzungskopie handelt (default null, wenn es noch unbekannt ist).', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

-- *** View aktualisieren ***
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
	IIF(i.Signatur IS NOT NULL,
		i.Signatur, 
		CONCAT(i.Bestand, '#', i.Ablieferung, ', ', IIF(i.BehaeltnisNummer IS NOT NULL,
														CONCAT('Bd. ', i.BehaeltnisNummer),
														CONCAT('Nr. ', i.ArchivNummer)))) as Signatur,
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
	i.Benutzungskopie,
	o.Eingangsart,
	i.Ausleihdauer,
	DATEADD(DAY, i.Ausleihdauer, o.orderDate) AS ErwartetesRueckgabeDatum,
	i.Ausgabedatum,
	i.Abschlussdatum,
	i.Mahndatum,
	i.AnzahlMahnungen,
	i.EntscheidGesuch,
	i.DatumDesEntscheids,
	o.BegruendungEinsichtsgesuch,
	i.HasEigenePersonendaten as UnterlagenDieNutzerSelberBetreffen,
	i.Abbruchgrund,
	i.HasPersonendaten as PersonenbezogeneNachforschung,
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
	o.orderDate  IS NOT NULL AND o.[Type] <> 0;

