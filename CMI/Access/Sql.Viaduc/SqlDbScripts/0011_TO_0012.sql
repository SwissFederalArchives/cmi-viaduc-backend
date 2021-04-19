-- ***** Tabelle News hinzufügen  *****

CREATE TABLE [dbo].[News](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FromDate] DATETIME NOT NULL,
	[ToDate] DATETIME NOT NULL,
	[De] VARCHAR(MAX) NOT NULL,
	[En] VARCHAR(MAX) NOT NULL,
	[Fr] VARCHAR(MAX) NOT NULL,
	[It] VARCHAR(MAX) NOT NULL
	CONSTRAINT [PK_News] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

EXEC sys.sp_addextendedproperty @level1name=N'News', @value=N'Enthält anzuzeigende Neuigkeiten und Meldungen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'

EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'Id', @value=N'News.Id', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'FromDate', @value=N'Enthält das Datum, ab welchem die Nachricht anzuzeigen ist', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'ToDate', @value=N'Enthält das Datum, bis zu welchem die Nachricht anzuzeigen ist', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'De', @value=N'Enthält die Nachricht in Deutsch', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'En', @value=N'Enthält die Nachricht in Englisch', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'Fr', @value=N'Enthält die Nachricht in Französisch', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'News', @level2name=N'It', @value=N'Enthält die Nachricht in Italienisch', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
