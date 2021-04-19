
-- ***** Script File für Bestellungen  *****

ALTER TABLE [dbo].[OrderItem] ALTER COLUMN Ve INT NULL

ALTER TABLE [dbo].[OrderItem] ADD [Bestand] NVARCHAR(MAX) NULL
ALTER TABLE [dbo].[OrderItem] ADD [Ablieferung] NVARCHAR(MAX) NULL
ALTER TABLE [dbo].[OrderItem] ADD [BehaeltnisNummer] NVARCHAR(MAX) NULL
ALTER TABLE [dbo].[OrderItem] ADD [Aktenzeichen] NVARCHAR(MAX) NULL
ALTER TABLE [dbo].[OrderItem] ADD [Dossiertitel] NVARCHAR(MAX) NULL
ALTER TABLE [dbo].[OrderItem] ADD [ZeitraumDossier] NVARCHAR(MAX) NULL

ALTER TABLE [dbo].[OrderItem] ADD [HasPersonendaten] BIT NOT NULL DEFAULT 0


ALTER TABLE [dbo].[Ordering] ADD [Comment] NVARCHAR(MAX) NULL
ALTER TABLE [dbo].[Ordering] ADD [ArtDerArbeit] INT NULL
ALTER TABLE [dbo].[Ordering] ADD [LesesaalDate] Date NULL


CREATE TABLE [dbo].[ArtDerArbeit](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name_de] [nvarchar](255) NOT NULL,
	[Name_fr] [nvarchar](255) NOT NULL,
	[Name_it] [nvarchar](255) NOT NULL,
	[Name_en] [nvarchar](255) NOT NULL
 CONSTRAINT [PK_ArtDerArbeit] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]


ALTER TABLE [dbo].[Ordering]  WITH CHECK ADD  CONSTRAINT [FK_Orderering_ArtDerArbeit] FOREIGN KEY([ArtDerArbeit])
  REFERENCES [dbo].[ArtDerArbeit] ([ID])

ALTER TABLE [dbo].[Ordering] CHECK CONSTRAINT [FK_Orderering_ArtDerArbeit]


ALTER TABLE [dbo].[OrderItem]  WITH CHECK ADD  CONSTRAINT [FK_OrderItem_Reason] FOREIGN KEY([Reason])
  REFERENCES [dbo].[Reason] ([ID])

ALTER TABLE [dbo].[OrderItem] CHECK CONSTRAINT [FK_OrderItem_Reason]


INSERT INTO [dbo].[ArtDerArbeit] ([Name_de], [Name_fr], [Name_it], [Name_en]) VALUES 
  ('01 Private Nachforschung', '(!fr) 01 Private Nachforschung', '(!it) 01 Private Nachforschung', '(!en) 01 Private Nachforschung'), 
  ('02 Journalistisch-publizistische Recherche', '(!fr) 02 Journalistisch-publizistische Recherche', '(!it) 02 Journalistisch-publizistische Recherche', '(!en) 02 Journalistisch-publizistische Recherche'), 
  ('03 Seminararbeit / Bachelorarbeit', '(!fr) 03 Seminararbeit / Bachelorarbeit', '(!it) 03 Seminararbeit / Bachelorarbeit', '(!en) 03 Seminararbeit / Bachelorarbeit'),
  ('04 Lizenziat / Masterarbeit', '(!fr) 04 Lizenziat / Masterarbeit', '(!it) 04 Lizenziat / Masterarbeit', '(!en) 04 Lizenziat / Masterarbeit'),
  ('05 Diplomarbeit', '(!fr) 05 Diplomarbeit', '(!it) 05 Diplomarbeit', '(!en) 05 Diplomarbeit'),
  ('06 Dissertation', '(!fr) 06 Dissertation', '(!it) 06 Dissertation', '(!en) 06 Dissertation'),
  ('07 Habilitation', '(!fr) 07 Habilitation', '(!it) 07 Habilitation', '(!en) 07 Habilitation'),
  ('08 Forschungsprojekt', '(!fr) 08 Forschungsprojekt', '(!it) 08 Forschungsprojekt', '(!en) 08 Forschungsprojekt'),
  ('09 Amtlicher Auftrag', '(!fr) 09 Amtlicher Auftrag', '(!it) 09 Amtlicher Auftrag', '(!en) 09 Amtlicher Auftrag'),
  ('10 Andere Arbeit', '(!fr) 10 Andere Arbeit', '(!it) 10 Andere Arbeit', '(!en) 10 Andere Arbeit')


-- Berschreibungen hinzufügen 
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'Bestand', @value=N'Feld für die Erfassung ohne Signatur', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'Ablieferung', @value=N'Feld für die Erfassung ohne Signatur', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'BehaeltnisNummer', @value=N'Feld für die Erfassung ohne Signatur', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'Aktenzeichen', @value=N'Feld für die Erfassung ohne Signatur', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'Dossiertitel', @value=N'Feld für die Erfassung ohne Signatur', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'ZeitraumDossier', @value=N'Feld für die Erfassung ohne Signatur', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'Ve', @value=N'Feld für die Erfassung mit Signatur', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'HasPersonendaten', @value=N'Bei der Bestellung muss teilweise angegeben werden, ob die Unterlagen Personendaten enthalten. Diese Angabe wird dann in diesem Feld abgelegt.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'Ordering', @level2name=N'LesesaalDate', @value=N'Datum an dem der Benutzer die Unterlagen im Lesesaal konsultieren möchte', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'Reason', @value=N'Gründe gemäss Art. 14 BGA', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'
