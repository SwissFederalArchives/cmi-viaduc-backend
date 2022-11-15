/* ---------------------------------------------------------------------- */
/* Add table "dbo.ManuelleKorrektur"                                             */
/* ---------------------------------------------------------------------- */
GO

CREATE TABLE [ManuelleKorrektur](
	[ManuelleKorrekturId] INTEGER IDENTITY(1,1) NOT NULL,
	[VeId] INTEGER NOT NULL,
	[Signatur] NVARCHAR(max) NOT NULL,
	[Schutzfristende] DATETIME2 NOT NULL,
	[Titel] NVARCHAR(max) NOT NULL,
	[ErzeugtAm] DATETIME2 DEFAULT getdate() NOT NULL,
	[ErzeugtVon] NVARCHAR(200) NOT NULL,
	[GeändertAm] DATETIME2 DEFAULT getdate() NULL,
	[GeändertVon] NVARCHAR(200) NULL,
	[Anonymisierungsstatus] INTEGER NOT NULL,
	[Kommentar] NVARCHAR(max) NULL,
	[Hierachiestufe] NVARCHAR(100) NOT NULL,
	[Aktenzeichen] NVARCHAR(200) NOT NULL,
	[Entstehungszeitraum] NVARCHAR(100) NULL,
	[ZugänglichkeitGemässBGA] NVARCHAR(100) NOT NULL,
	[Schutzfristverzeichnung] NVARCHAR(200) NULL,
	[ZuständigeStelle] NVARCHAR(100) NULL,
	[Publikationsrechte] NVARCHAR(200) NULL,
	[AnonymisiertZumErfassungszeitpunk] BIT NOT NULL,
 CONSTRAINT [PK_ManuelleKorrektur] PRIMARY KEY CLUSTERED 
(
	[ManuelleKorrekturId] ASC
))
GO


CREATE TABLE [ManuelleKorrekturStatusHistory](
	[ManuelleKorrekturStatusHistoryId] INTEGER IDENTITY(1,1) NOT NULL,
	[ManuelleKorrekturId] INTEGER NOT NULL,
	[Anonymisierungsstatus] INTEGER NOT NULL,
	[ErzeugtAm] DATETIME2 NOT NULL,
	[ErzeugtVon] NVARCHAR(200) NOT NULL,
 CONSTRAINT [PK_ManuelleKorrekturStatusHistory] PRIMARY KEY CLUSTERED 
(
	[ManuelleKorrekturStatusHistoryId] ASC
))
GO

ALTER TABLE [ManuelleKorrekturStatusHistory]  WITH CHECK ADD  CONSTRAINT [FK_ManuelleKorrekturStatusHistory_ManuelleKorrektur1] FOREIGN KEY([ManuelleKorrekturId])
REFERENCES [ManuelleKorrektur] ([ManuelleKorrekturId])
ON DELETE CASCADE
GO

ALTER TABLE [ManuelleKorrekturStatusHistory] CHECK CONSTRAINT [FK_ManuelleKorrekturStatusHistory_ManuelleKorrektur1]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [ManuelleKorrekturFeld](
	[ManuelleKorrekturFelderId] INTEGER IDENTITY(1,1) NOT NULL,
	[ManuelleKorrekturId] INTEGER NOT NULL,
	[Feldname] NVARCHAR(200) NOT NULL,
	[Original] NVARCHAR(max) NULL,
	[Automatisch] NVARCHAR(max) NULL,
	[Manuell] NVARCHAR(max) NULL,
 CONSTRAINT [PK_ManuelleKorrekturFelder] PRIMARY KEY CLUSTERED 
(
	[ManuelleKorrekturFelderId] ASC
))
GO

ALTER TABLE [ManuelleKorrekturFeld]  WITH CHECK ADD  CONSTRAINT [FK_ManuelleKorrekturFelder_ManuelleKorrektur] FOREIGN KEY([ManuelleKorrekturID])
REFERENCES [ManuelleKorrektur] ([ManuelleKorrekturId])
ON DELETE CASCADE
GO

ALTER TABLE [ManuelleKorrekturFeld] CHECK CONSTRAINT [FK_ManuelleKorrekturFelder_ManuelleKorrektur]
GO

CREATE  OR ALTER View v_ManuelleKorrektur
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
                            WHERE      (Feldname = 'Zusatzkomponente') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusatzkomponenteGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_8
                            WHERE      (Feldname = 'Zusatzkomponente') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusatzkomponenteAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_9
                            WHERE      (Feldname = 'Zusatzkomponente') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusatzkomponenteManuellKorrigiert,
                          (SELECT    Original
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_10
                            WHERE      (Feldname = 'ZusaetzlicheInformationen') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusaetzlicheInformationenGemAIS,
                          (SELECT    Automatisch
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_11
                            WHERE      (Feldname = 'ZusaetzlicheInformationen') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusaetzlicheInformationenAutomatischAnonymisiert,
                          (SELECT    Manuell
                            FROM         dbo.ManuelleKorrekturFeld AS ManuelleKorrekturFeld_12
                            WHERE      (Feldname = 'ZusaetzlicheInformationen') AND (ManuelleKorrekturId = dbo.ManuelleKorrektur.ManuelleKorrekturId)) AS ZusaetzlicheInformationenManuellKorrigiert,
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

CREATE UNIQUE NONCLUSTERED INDEX [IX_ManuelleKorrekturFeld] ON [ManuelleKorrekturFeld]
(
	[Feldname] ASC,
	[ManuelleKorrekturId] ASC
)
GO

