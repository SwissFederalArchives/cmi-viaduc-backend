/* Bestehenden Constraints löschen */
BEGIN TRY  
	ALTER TABLE Favorite
	DROP CONSTRAINT PK_favorite
END TRY  
BEGIN CATCH  
END CATCH

BEGIN TRY  
	ALTER TABLE Favorite
	DROP CONSTRAINT PK_Favorite_List
END TRY  
BEGIN CATCH  
END CATCH   
GO

/* Neue Favoriten Tabelle erzeugen */
CREATE TABLE [dbo].[tmp_favorite] (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Ve] INT,
	[Kind] TINYINT NOT NULL DEFAULT 1,
	[CreatedAt] DATETIME NOT NULL DEFAULT SYSDATETIME(),
	[Title] VARCHAR(MAX),
	[Url] VARCHAR(MAX),
	[List] INT,
	CONSTRAINT [PK_favorite] PRIMARY KEY CLUSTERED 
	(
		[ID] ASC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]


/* Alte Tabelle migrieren  */
INSERT INTO tmp_favorite (VE, List, Kind) SELECT Ve, List, 1 as Kind FROM Favorite;

/* Alte Tabelle löschen, neue Tabelle umbenennen */
DROP TABLE Favorite
GO
EXEC sp_rename 'dbo.tmp_favorite', 'Favorite'

/* Constraints neu erstellen Favorite -> List */
ALTER TABLE [dbo].[Favorite] WITH CHECK 
ADD CONSTRAINT [FK_Favorite_List]
FOREIGN KEY([List]) REFERENCES [dbo].[FavoriteList] ([ID])

ALTER TABLE [dbo].[Favorite] CHECK CONSTRAINT [FK_Favorite_List]

/* Kommentare aktualisieren */
EXECUTE sp_addextendedproperty N'MS_Description', N'Unique Identifier', N'SCHEMA', N'dbo', N'TABLE', N'Favorite', N'COLUMN', N'ID'
EXECUTE sp_addextendedproperty N'MS_Description', N'ID der Verzeichniseinheit (falls vorhanden)', N'SCHEMA', N'dbo', N'TABLE', N'Favorite', N'COLUMN', N'Ve'
EXECUTE sp_addextendedproperty N'MS_Description', N'Art (1 = Verzeichniseinheit, 2 = Suchabfrage)', N'SCHEMA', N'dbo', N'TABLE', N'Favorite', N'COLUMN', N'Kind'
EXECUTE sp_addextendedproperty N'MS_Description', N'Titel der Suchabfrage', N'SCHEMA', N'dbo', N'TABLE', N'Favorite', N'COLUMN', N'Title'
EXECUTE sp_addextendedproperty N'MS_Description', N'URL der Suchabfrage', N'SCHEMA', N'dbo', N'TABLE', N'Favorite', N'COLUMN', N'Url'
EXECUTE sp_addextendedproperty N'MS_Description', N'Fremdschlüssel auf Liste', N'SCHEMA', N'dbo', N'TABLE', N'Favorite', N'COLUMN', N'List'
