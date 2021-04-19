--INFO: Dokumentation der Felder und Tabellen erfolgt ganz am Schluss

-- ---------------------------------------------------------------------------------------
-- Neue Spalte
-- ---------------------------------------------------------------------------------------
ALTER TABLE [dbo].[ApplicationUser] ADD [UserExtId] NVARCHAR(100) NULL
GO

-- ---------------------------------------------------------------------------------------
-- Neue Tabellen 
-- ---------------------------------------------------------------------------------------
-- Tabellen
CREATE TABLE [dbo].[AblieferndeStelle]
(
	AblieferndeStelleId INT IDENTITY(1,1) NOT NULL
		CONSTRAINT PK_AblieferndeStelle PRIMARY KEY CLUSTERED,
	Bezeichnung VARCHAR(255) NOT NULL,
	Kuerzel VARCHAR(255) NOT NULL,
);
GO

CREATE TABLE [dbo].[AblieferndeStelleToken]
(
	TokenId INT IDENTITY(1,1) NOT NULL
		CONSTRAINT PK_AblieferndeStelleToken PRIMARY KEY CLUSTERED,
	Token NVARCHAR(50)  NOT NULL,
	Bezeichnung VARCHAR(255) NOT NULL,
);
GO

-- Zwischen Tabellen
CREATE TABLE [dbo].[ApplicationUserAblieferndeStelle]
(
	UserId NVARCHAR(200) NOT NULL
		CONSTRAINT FK_ApplicationUserAblieferndeStelleUser
		FOREIGN KEY REFERENCES dbo.ApplicationUser(ID),			

	AblieferndeStelleId INT NOT NULL
		CONSTRAINT FK_ApplicationUserAblieferndeStelleAblieferndeStelle
		FOREIGN KEY REFERENCES dbo.AblieferndeStelle(AblieferndeStelleId),

	PRIMARY KEY (UserId, AblieferndeStelleId)
);
GO

CREATE TABLE [dbo].[AsTokenMapping]
(
	AblieferndeStelleId INT NOT NULL
		CONSTRAINT FK_AsTokenMappingAblieferndeStelle
		FOREIGN KEY REFERENCES dbo.AblieferndeStelle(AblieferndeStelleId),			

	TokenId INT NOT NULL
		CONSTRAINT FK_AsTokenMappingToken
		FOREIGN KEY REFERENCES dbo.AblieferndeStelleToken(TokenId),

	PRIMARY KEY (AblieferndeStelleId, TokenId)
);
GO

-- ---------------------------------------------------------------------------------------
-- Dokumentation
-- ---------------------------------------------------------------------------------------

EXEC sys.sp_addextendedproperty 'MS_Description', 'Vom eIAM Benutzer externe ID' , 'SCHEMA', 'dbo', 'TABLE', 'ApplicationUser', 'COLUMN', UserExtId
EXEC sys.sp_addextendedproperty 'MS_Description', 'Tabelle zum darstellen der Ämter' , 'SCHEMA', 'dbo', 'TABLE', 'AblieferndeStelle'
EXEC sys.sp_addextendedproperty 'MS_Description', 'PrimaryKey "auto inc." ' , 'SCHEMA', 'dbo', 'TABLE', 'AblieferndeStelle', 'COLUMN', AblieferndeStelleId
EXEC sys.sp_addextendedproperty 'MS_Description', 'Bezeichnung des Amts Bsp. "Bundesamt für Statistik"' , 'SCHEMA', 'dbo', 'TABLE', 'AblieferndeStelle', 'COLUMN', Bezeichnung
EXEC sys.sp_addextendedproperty 'MS_Description', 'Kürzel des Amts Bsp. "BFS"' , 'SCHEMA', 'dbo', 'TABLE', 'AblieferndeStelle', 'COLUMN', Kuerzel
EXEC sys.sp_addextendedproperty 'MS_Description', 'Tabelle um die Access Token darzustellen ' , 'SCHEMA', 'dbo', 'TABLE', 'AblieferndeStelleToken'
EXEC sys.sp_addextendedproperty 'MS_Description', 'PrimaryKey "auto inc.' , 'SCHEMA', 'dbo', 'TABLE', 'AblieferndeStelleToken', 'COLUMN', TokenId
EXEC sys.sp_addextendedproperty 'MS_Description', 'Token Bsp. "AS_8897"' , 'SCHEMA', 'dbo', 'TABLE', 'AblieferndeStelleToken', 'COLUMN', Token
EXEC sys.sp_addextendedproperty 'MS_Description', 'Bezeichnung des Tokens Bsp. "Bundesamt für Statistik"' , 'SCHEMA', 'dbo', 'TABLE', 'AblieferndeStelleToken', 'COLUMN', Bezeichnung
EXEC sys.sp_addextendedproperty 'MS_Description', 'Zwischentabelle (n:n Verbindung) zwischen "ApplictionUser" und "AblieferndeStelle"' , 'SCHEMA', 'dbo', 'TABLE', 'ApplicationUserAblieferndeStelle'
EXEC sys.sp_addextendedproperty 'MS_Description', 'Foreign key zu ApplictionUser.ID ist ein PrimaryKey' , 'SCHEMA', 'dbo', 'TABLE', 'ApplicationUserAblieferndeStelle', 'COLUMN', UserId
EXEC sys.sp_addextendedproperty 'MS_Description', 'Foreign key zu AbliefendeStelle.AbliefendeStelleID ist ein PrimaryKey ' , 'SCHEMA', 'dbo', 'TABLE', 'ApplicationUserAblieferndeStelle', 'COLUMN', AblieferndeStelleId
EXEC sys.sp_addextendedproperty 'MS_Description', 'Zwischentabelle (n:n Verbindung) zwischen "AblieferndeStelle" und "AblieferndeStelleToken"' , 'SCHEMA', 'dbo', 'TABLE', 'AsTokenMapping'
EXEC sys.sp_addextendedproperty 'MS_Description', 'Foreign key zu AbliefendeStelle.AbliefendeStelleID ist ein PrimaryKey' , 'SCHEMA', 'dbo', 'TABLE', 'AsTokenMapping', 'COLUMN', AblieferndeStelleId
EXEC sys.sp_addextendedproperty 'MS_Description', 'Foreign key zu AbliefendeStelleToken.TokenId ist ein PrimaryKey' , 'SCHEMA', 'dbo', 'TABLE', 'AsTokenMapping', 'COLUMN', TokenId

GO
