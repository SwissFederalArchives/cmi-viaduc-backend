
/* ---------------------------------------------------------------------- */
/* Alter table "dbo.OrderItem"                                            */
/* ---------------------------------------------------------------------- */


ALTER TABLE [dbo].[OrderItem] ALTER COLUMN [Mahndatum] NVARCHAR(max)
GO

EXEC sp_rename '[dbo].[OrderItem].[Mahndatum]', 'MahndatumInfo', 'COLUMN'
GO

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
	i.MahndatumInfo,
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
    END AS ExternalStatus,
	i.HasAufbereitungsfehler,
	o.RolePublicClient,
	i.GebrauchskopieStatus
FROM
	OrderItem i
	INNER JOIN Ordering o ON i.OrderId = o.ID
	INNER JOIN ApplicationUser u ON u.ID = o.UserId
	LEFT JOIN Reason r ON r.ID = i.Reason
	LEFT JOIN ArtDerArbeit a ON a.ID = o.ArtDerArbeit
WHERE
	o.orderDate  IS NOT NULL AND o.[Type] <> 0;