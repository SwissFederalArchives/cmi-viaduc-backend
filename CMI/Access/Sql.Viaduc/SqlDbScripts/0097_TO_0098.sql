/* ---------------------------------------------------------------------- */
/* Script changes the integer datatype of the VeId field to navarchar     */
/* Created on:            2024-12-17 15:08                                */
/* ---------------------------------------------------------------------- */


/* ---------------------------------------------------------------------- */
/* Drop foreign key constraints                                           */
/* ---------------------------------------------------------------------- */

ALTER TABLE [DownloadReasonHistory] DROP CONSTRAINT [FK_DownloadReasonHistory_Reason]
GO


ALTER TABLE [DownloadReasonHistory] DROP CONSTRAINT [FK_DownloadReasonHistory_User]
GO


ALTER TABLE [DownloadToken] DROP CONSTRAINT [FK_DownloadToken_User]
GO


ALTER TABLE [Favorite] DROP CONSTRAINT [FK_Favorite_List]
GO


ALTER TABLE [OrderItem] DROP CONSTRAINT [FK_OrderItem_Reason]
GO


ALTER TABLE [OrderItem] DROP CONSTRAINT [FK_OrderItem_Order]
GO


ALTER TABLE [OrderItem] DROP CONSTRAINT [FK_OrderItem_Sachbearbeiter_User]
GO


ALTER TABLE [ManuelleKorrekturFeld] DROP CONSTRAINT [FK_ManuelleKorrekturFelder_ManuelleKorrektur]
GO


ALTER TABLE [ManuelleKorrekturStatusHistory] DROP CONSTRAINT [FK_ManuelleKorrekturStatusHistory_ManuelleKorrektur1]
GO


ALTER TABLE [PrimaerdatenAuftragLog] DROP CONSTRAINT [PrimaerdatenAuftrag_PrimaerdatenAuftragLog]
GO


ALTER TABLE [StatusHistory] DROP CONSTRAINT [OrderItem_StatusHistory]
GO


/* ---------------------------------------------------------------------- */
/* Alter table "dbo.DownloadReasonHistory"                                */
/* ---------------------------------------------------------------------- */

ALTER TABLE [DownloadReasonHistory] ALTER COLUMN [VeId] NVARCHAR(255) NOT NULL
GO


/* ---------------------------------------------------------------------- */
/* Alter table "dbo.DownloadToken"                                        */
/* ---------------------------------------------------------------------- */

ALTER TABLE [DownloadToken] ALTER COLUMN [RecordId] NVARCHAR(255) NOT NULL
GO


/* ---------------------------------------------------------------------- */
/* Alter table "dbo.Favorite"                                             */
/* ---------------------------------------------------------------------- */

ALTER TABLE [Favorite] ALTER COLUMN [Ve] NVARCHAR(255)
GO


/* ---------------------------------------------------------------------- */
/* Alter table "dbo.ManuelleKorrektur"                                    */
/* ---------------------------------------------------------------------- */

ALTER TABLE [ManuelleKorrektur] ALTER COLUMN [VeId] NVARCHAR(255) NOT NULL
GO


/* ---------------------------------------------------------------------- */
/* Alter table "dbo.OrderExecutedWaitList"                                */
/* ---------------------------------------------------------------------- */

DROP INDEX [OrderExecutedWaitList].[IDX_OrderExecutedWaitList_1]
GO


ALTER TABLE [OrderExecutedWaitList] ALTER COLUMN [VeId] NVARCHAR(255) NOT NULL
GO


CREATE NONCLUSTERED INDEX [IDX_OrderExecutedWaitList_1] ON [OrderExecutedWaitList] ([VeId] ASC)
GO


/* ---------------------------------------------------------------------- */
/* Alter table "dbo.OrderItem"                                            */
/* ---------------------------------------------------------------------- */

DROP INDEX [OrderItem].[IX_ForCalcIndividualAccessTokens]
GO


ALTER TABLE [OrderItem] DROP CONSTRAINT [CK_OrderItem_EntwederEntscheidOderFreigabe]
GO


ALTER TABLE [OrderItem] ALTER COLUMN [Ve] NVARCHAR(255)
GO


ALTER TABLE [OrderItem] ADD CONSTRAINT [CK_OrderItem_EntwederEntscheidOderFreigabe] 
    CHECK ([EntscheidGesuch]=(0) OR [ApproveStatus]=(0))
GO


CREATE NONCLUSTERED INDEX [IX_ForCalcIndividualAccessTokens] ON [OrderItem] ([Ve] ASC,[ApproveStatus] ASC,[EntscheidGesuch] ASC) INCLUDE (ID,OrderId) 
GO


/* ---------------------------------------------------------------------- */
/* Alter table "dbo.PrimaerdatenAuftrag"                                  */
/* ---------------------------------------------------------------------- */

DROP INDEX [PrimaerdatenAuftrag].[IX_PrimaerdatenAuftrag_4]
GO


ALTER TABLE [PrimaerdatenAuftrag] ALTER COLUMN [VeId] NVARCHAR(255) NOT NULL
GO


CREATE NONCLUSTERED INDEX [IX_PrimaerdatenAuftrag_4] ON [PrimaerdatenAuftrag] ([Abgeschlossen] ASC,[VeId] ASC,[Status] ASC)
GO


/* ---------------------------------------------------------------------- */
/* Alter table "dbo.TempMigrationWorkspace"                               */
/* ---------------------------------------------------------------------- */

ALTER TABLE [TempMigrationWorkspace] ALTER COLUMN [Verzeichnungseinheit_Id] NVARCHAR(255) NOT NULL
GO


/* ---------------------------------------------------------------------- */
/* Add foreign key constraints                                            */
/* ---------------------------------------------------------------------- */

ALTER TABLE [DownloadReasonHistory] ADD CONSTRAINT [FK_DownloadReasonHistory_Reason] 
    FOREIGN KEY ([ReasonId]) REFERENCES [Reason] ([ID])
GO


ALTER TABLE [DownloadReasonHistory] ADD CONSTRAINT [FK_DownloadReasonHistory_User] 
    FOREIGN KEY ([UserId]) REFERENCES [ApplicationUser] ([ID])
GO


ALTER TABLE [DownloadToken] ADD CONSTRAINT [FK_DownloadToken_User] 
    FOREIGN KEY ([UserId]) REFERENCES [ApplicationUser] ([ID])
GO


ALTER TABLE [Favorite] ADD CONSTRAINT [FK_Favorite_List] 
    FOREIGN KEY ([List]) REFERENCES [FavoriteList] ([ID])
GO


ALTER TABLE [OrderItem] ADD CONSTRAINT [FK_OrderItem_Reason] 
    FOREIGN KEY ([Reason]) REFERENCES [Reason] ([ID])
GO


ALTER TABLE [OrderItem] ADD CONSTRAINT [FK_OrderItem_Order] 
    FOREIGN KEY ([OrderId]) REFERENCES [Ordering] ([ID])
GO


ALTER TABLE [OrderItem] ADD CONSTRAINT [FK_OrderItem_Sachbearbeiter_User] 
    FOREIGN KEY ([SachbearbeiterId]) REFERENCES [ApplicationUser] ([ID])
GO


ALTER TABLE [ManuelleKorrekturFeld] ADD CONSTRAINT [FK_ManuelleKorrekturFelder_ManuelleKorrektur] 
    FOREIGN KEY ([ManuelleKorrekturId]) REFERENCES [ManuelleKorrektur] ([ManuelleKorrekturId]) ON DELETE CASCADE
GO


ALTER TABLE [ManuelleKorrekturStatusHistory] ADD CONSTRAINT [FK_ManuelleKorrekturStatusHistory_ManuelleKorrektur1] 
    FOREIGN KEY ([ManuelleKorrekturId]) REFERENCES [ManuelleKorrektur] ([ManuelleKorrekturId]) ON DELETE CASCADE
GO


ALTER TABLE [PrimaerdatenAuftragLog] ADD CONSTRAINT [PrimaerdatenAuftrag_PrimaerdatenAuftragLog] 
    FOREIGN KEY ([PrimaerdatenAuftragId]) REFERENCES [PrimaerdatenAuftrag] ([PrimaerdatenAuftragId]) ON DELETE CASCADE
GO


ALTER TABLE [StatusHistory] ADD CONSTRAINT [OrderItem_StatusHistory] 
    FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItem] ([ID])
GO

/* ---------------------------------------------------------------------- */
/* Rebuild the views                                                      */
/* ---------------------------------------------------------------------- */


CREATE OR ALTER    View v_ManuelleKorrektur
AS
SELECT    ManuelleKorrekturId, VeId, Signatur, Schutzfristende, Titel, ErzeugtAm, ErzeugtVon, GeändertAm, GeändertVon, Anonymisierungsstatus, Kommentar, Hierachiestufe, Aktenzeichen, Entstehungszeitraum, 
                      ZugänglichkeitGemässBGA, Schutzfristverzeichnung, ZuständigeStelle, Publikationsrechte, AnonymisiertZumErfassungszeitpunk,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_1
                            WHERE      (Feldname = 'Titel') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS TitelGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_2
                            WHERE      (Feldname = 'Titel') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS TitelAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_3
                            WHERE      (Feldname = 'Titel') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS TitelManuellKorrigiert,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_4
                            WHERE      (Feldname = 'Darin') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS DarinGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_5
                            WHERE      (Feldname = 'Darin') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS DarinAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_6
                            WHERE      (Feldname = 'Darin') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS DarinManuellKorrigiert,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_7
                            WHERE      (Feldname = 'ZusatzkomponenteZac1') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusatzkomponenteGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_8
                            WHERE      (Feldname = 'ZusatzkomponenteZac1') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusatzkomponenteAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_9
                            WHERE      (Feldname = 'ZusatzkomponenteZac1') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusatzkomponenteManuellKorrigiert,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_10
                            WHERE      (Feldname = 'BemerkungZurVe') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusaetzlicheInformationenGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_11
                            WHERE      (Feldname = 'BemerkungZurVe') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusaetzlicheInformationenAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_12
                            WHERE      (Feldname = 'BemerkungZurVe') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusaetzlicheInformationenManuellKorrigiert,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_13
                            WHERE      (Feldname = 'VerwandteVE') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS VerwandteVEGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_14
                            WHERE      (Feldname = 'VerwandteVE') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS VerwandteVEAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_15
                            WHERE      (Feldname = 'VerwandteVE') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS VerwandteVEManuellKorrigiert
FROM         dbo.ManuelleKorrektur
GO


CREATE OR ALTER VIEW v_OrderingFlatItem AS
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
	DATEADD(DAY, i.Ausleihdauer, i.Ausgabedatum) AS ErwartetesRueckgabeDatum,
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
	u.FamilyName + ', ' + u.Firstname + IIF(u.Organization IS NOT NULL, ', ' + u.Organization, '') As [User],
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
GO


CREATE OR ALTER   View v_Primaerdatenaufbereitung
AS
SELECT    
	ofi.OrderingDate, 
	ofi.OrderingType, 
	ofi.ItemId as OrderItemId, 
	ofi.Dossiertitel,
	PrimaerdatenAuftrag.VeId,
	ofi.DigitalisierungsKategorie as DigitalisierungsKategorieId,
	ofi.Signatur,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 1) AS NeuEingegangen,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 9) AS Ausgeliehen,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 12) AS ZumReponierenBereit,
	(Select max(CreatedOn) from PrimaerdatenAuftragLog where PrimaerdatenAuftragId = PrimaerdatenAuftrag.PrimaerdatenAuftragId and Status = 'AuftragErledigt') AS AuftragErledigt,
	CASE PrimaerdatenAuftrag.AufbereitungsArt 
	WHEN 'Sync' THEN
		    (Select min(AbgeschlossenAm) from PrimaerdatenAuftrag pda where VeId = PrimaerdatenAuftrag.VeId and pda.createdOn > PrimaerdatenAuftrag.CreatedOn and pda.AufbereitungsArt = 'Download')
	ELSE 
		    (Select min(AbgeschlossenAm) from PrimaerdatenAuftrag pda where VeId = PrimaerdatenAuftrag.VeId and pda.PrimaerdatenAuftragId = PrimaerdatenAuftrag.PrimaerdatenAuftragId and pda.AufbereitungsArt = 'Download')
	END as Abgeschlossen,
	PrimaerdatenAuftrag.PrimaerdatenAuftragId,
	PrimaerdatenAuftrag.AufbereitungsArt, 
	PrimaerdatenAuftrag.GroesseInBytes,
	PrimaerdatenAuftrag.GeschaetzteAufbereitungszeit,
	CASE 
		WHEN Charindex('"FileCount":', packagemetadata) > 0 THEN 
		Substring(packagemetadata, Charindex('"FileCount":', packagemetadata) + Len('"FileCount":'),
		Charindex(',"PackageId":', packagemetadata) - Charindex('"FileCount":', packagemetadata) - Len('"FileCount":'))
		ELSE 0
		END AS AnzahlDateien
 FROM PrimaerdatenAuftrag
  INNER JOIN v_OrderingFlatItem AS ofi
    ON PrimaerdatenAuftrag.VeId = ofi.VeId
			AND ofi.OrderId = (Select min(j.OrderId)
                                       from v_OrderingFlatItem j
                                       where j.VeId         = PrimaerdatenAuftrag.VeId
                                         and j.OrderingDate = (Select max(i.OrderingDate)
                                                               from v_OrderingFlatItem i
                                                               where i.VeId         = PrimaerdatenAuftrag.VeId
                                                                 and i.OrderingDate < PrimaerdatenAuftrag.CreatedOn
                                                               group by i.veid))
GO


