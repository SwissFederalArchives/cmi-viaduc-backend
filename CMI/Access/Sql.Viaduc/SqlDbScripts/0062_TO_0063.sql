-- ** Neue Spalten auf Ordering ** --
ALTER TABLE Ordering ADD PersonenbezogeneNachforschung BIT DEFAULT(0) NOT NULL
ALTER TABLE Ordering ADD HasEigenePersonendaten BIT DEFAULT(0) NOT NULL

EXEC sys.sp_addextendedproperty @level1name=N'Ordering', @level2name=N'PersonenbezogeneNachforschung', @value=N'Gibt an, ob das Einsichtsgesuch auf einer personenbezogene Nachforschung basiert.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'Ordering', @level2name=N'HasEigenePersonendaten', @value=N'Gibt an, ob das Einsichtsgesuch sich auf die eigenen Personendaten bezieht.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

GO

-- *** HasEigenePersonendaten von OrderItem auf Ordering übernehmen *** --
UPDATE Ordering 
	SET Ordering.HasEigenePersonendaten = OrderItem.HasEigenePersonendaten 
FROM OrderItem
	INNER JOIN Ordering 
ON Ordering.id = OrderItem.OrderId;

-- *** Spalte inkl. Default-Constraint aus OrderItem entfernen ** ---
DECLARE @TableName AS NVARCHAR(255),
     @ColumnName AS NVARCHAR(255),
     @ConstraintName AS NVARCHAR(255),
     @DropConstraintSQL AS NVARCHAR(255)

SET @TableName = 'OrderItem'
SET @ColumnName = 'HasEigenePersonendaten'

SET @ConstraintName = 
     (SELECT TOP 1 o.name FROM sysobjects o 
     JOIN syscolumns c 
     ON o.id = c.cdefault 
     JOIN sysobjects t 
     ON c.id = t.id 
     WHERE o.xtype = 'd' 
     AND c.name = @ColumnName 
     AND t.name = @TableName)

SET @DropConstraintSQL = 'ALTER TABLE ' + @TableName + ' DROP ' + @ConstraintName
EXEC (@DropConstraintSQL)

GO

ALTER TABLE OrderItem DROP COLUMN HasEigenePersonendaten;

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
    i.Signatur, 
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
	o.HasEigenePersonendaten as UnterlagenDieNutzerSelberBetreffen,
	i.Abbruchgrund,
	o.PersonenbezogeneNachforschung,
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