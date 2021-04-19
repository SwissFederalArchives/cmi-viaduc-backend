
ALTER TABLE [dbo].[DownloadToken] DROP CONSTRAINT [FK_DownloadToken_User]
GO


/* ---------------------------------------------------------------------- */
/* Drop and recreate table "dbo.DownloadToken"                            */
/* ---------------------------------------------------------------------- */

GO


ALTER TABLE [dbo].[DownloadToken] DROP CONSTRAINT [PK_FileToken]
GO


CREATE TABLE [dbo].[DownloadToken_TMP] (
    [Token] NVARCHAR(100) NOT NULL,
    [TokenType] NVARCHAR(50) NOT NULL,
    [RecordId] INTEGER NOT NULL,
    [ExpiryTime] DATETIME2 NOT NULL,
    [IpAdress] NVARCHAR(max) NOT NULL,
    [UserId] NVARCHAR(200) NOT NULL)
GO

DROP TABLE [dbo].[DownloadToken]
GO

EXEC sp_rename '[dbo].[DownloadToken_TMP]', 'DownloadToken', 'OBJECT'
GO


ALTER TABLE [dbo].[DownloadToken] ADD CONSTRAINT [PK_FileToken] 
    PRIMARY KEY CLUSTERED ([Token])
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Das Token, welches für den Download verwendet werden muss.', 'SCHEMA', N'dbo', 'TABLE', N'DownloadToken', 'COLUMN', N'Token'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Gibt an, für was für eine Art Record es sich handelt, respektive für welche Ressource das Token gültig ist.', 'SCHEMA', N'dbo', 'TABLE', N'DownloadToken', 'COLUMN', N'TokenType'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Die ID der Ressource', 'SCHEMA', N'dbo', 'TABLE', N'DownloadToken', 'COLUMN', N'RecordId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Ablaufdatum des Tokens', 'SCHEMA', N'dbo', 'TABLE', N'DownloadToken', 'COLUMN', N'ExpiryTime'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Die IP Adresse des Benutzers welcher das Token angefordert hat.', 'SCHEMA', N'dbo', 'TABLE', N'DownloadToken', 'COLUMN', N'IpAdress'
GO


/* ---------------------------------------------------------------------- */
/* Add foreign key constraints                                            */
/* ---------------------------------------------------------------------- */

GO


ALTER TABLE [dbo].[DownloadToken] ADD CONSTRAINT [FK_DownloadToken_User] 
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[ApplicationUser] ([ID])
GO
