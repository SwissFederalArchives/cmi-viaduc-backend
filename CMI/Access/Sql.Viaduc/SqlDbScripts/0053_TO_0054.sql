CREATE TABLE DownloadLog
(
    Token nvarchar(100) not null,
    UserId nvarchar(200) not null,
    UserTokens nvarchar(max) not null,
    Vorgang nvarchar(10) not null,
    Signatur nvarchar(max) not null,
    Titel nvarchar(max) not null,
    Schutzfrist nvarchar(max) null,
    DatumErstellungToken dateTime not null,
	DatumVorgang dateTime null,
	CONSTRAINT PK_DownloadLog PRIMARY KEY CLUSTERED 
    (
        [Token] ASC
    ) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
ON [PRIMARY]


GO
ALTER TABLE [dbo].[DownloadLog]  WITH CHECK ADD  CONSTRAINT [FK_DownloadLog_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[ApplicationUser] ([ID])
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @value=N'Jeder Dateidownload wird in dieser Tabelle geloggt.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @level2name=N'Token',                @value=N'Token',                                                                                                   @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @level2name=N'UserId',               @value=N'ID des Benutzers, der den Download durchgeführt hat',                                                     @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @level2name=N'UserTokens',           @value=N'Rechte-Tokens des Benutzers zum Zeitpunkt des Downloads',                                                 @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @level2name=N'Vorgang',              @value=N'Download resp. Ansicht',                                                                                  @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @level2name=N'Signatur',             @value=N'Signatur der VE, deren Primärdaten heruntergeladen wurden',                                               @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @level2name=N'Titel',                @value=N'Titel der VE, deren Primärdaten heruntergeladen wurden',                                                  @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @level2name=N'Schutzfrist',          @value=N'Schutzfrist der VE, deren Primärdaten heruntergeladen wurden',                                            @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @level2name=N'DatumErstellungToken', @value=N'Datum der Erstellung des Tokens',                                                                         @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
GO
EXEC sys.sp_addextendedproperty @level1name=N'DownloadLog', @level2name=N'DatumVorgang',         @value=N'Datum, an welchem der Vorgang durchgeführt wurde. Welcher Vorgang das war, steht in der Spalte Vorgang.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'