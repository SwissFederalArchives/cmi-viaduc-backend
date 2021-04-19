-- ***** Zur Tabelle NEWS werden Header fuer die Sprachen hinzugefuegt  *****
-- ***** Das die Werte nicht leer sein duerfen, wird die Tabelle vorher geleert  *****

TRUNCATE TABLE [dbo].[News]

ALTER TABLE [dbo].[News]
	ADD [DeHeader] VARCHAR(MAX) NOT NULL,
		[EnHeader] VARCHAR(MAX) NOT NULL,
		[FrHeader] VARCHAR(MAX) NOT NULL,
		[ItHeader] VARCHAR(MAX) NOT NULL

EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'DeHeader', @value=N'Enthaelt die Ueberschrift in Deutsch', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'EnHeader', @value=N'Enthaelt die Ueberschrift in Englisch', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'FrHeader', @value=N'Enthaelt die Ueberschrift in Franzoesisch', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'ItHeader', @value=N'Enthaelt die Ueberschrift in Italienisch', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
