  
 DROP TABLE UserUsageStatistics; 


-- *** Neue Tabelle UsageStatisticDisplay ***

CREATE TABLE [dbo].[UsageStatisticDisplay](
	[UserId] [nvarchar](200) NOT NULL,
	[Usage] NVARCHAR(MAX) NULL,
 CONSTRAINT [PK_UsageStatisticDisplay] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[UsageStatisticDisplay]  WITH CHECK ADD  CONSTRAINT [FK_UsageStatisticDisplay_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[ApplicationUser] ([ID])

ALTER TABLE [dbo].[UsageStatisticDisplay] CHECK CONSTRAINT [FK_UsageStatisticDisplay_User]

EXEC sys.sp_addextendedproperty @level1name=N'UsageStatisticDisplay', @value=N'In der Tabelle wird gespeichert, wie viele Treffer(VEs) vom Benutzer angeforderten wurden.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'

EXEC sys.sp_addextendedproperty @level1name=N'UsageStatisticDisplay', @level2name=N'UserId', @value=N'ApplicationUser.Id', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'UsageStatisticDisplay', @level2name=N'Usage', @value=N'Json-Repräsentation der summarischen Benutzer Usage-Statistik', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'


-- *** Neue Tabelle UsageStatisticDownload ***

CREATE TABLE [dbo].[UsageStatisticDownload](
	[UserId] [nvarchar](200) NOT NULL,
	[Usage] NVARCHAR(MAX) NULL,
 CONSTRAINT [PK_UsageStatisticDownload] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[UsageStatisticDownload]  WITH CHECK ADD  CONSTRAINT [FK_UsageStatisticDownload_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[ApplicationUser] ([ID])

ALTER TABLE [dbo].[UsageStatisticDownload] CHECK CONSTRAINT [FK_UsageStatisticDownload_User]

EXEC sys.sp_addextendedproperty @level1name=N'UsageStatisticDownload', @value=N'In der Tabelle wird gespeichert, wie viele Downloads ein Benutzer ausgeführt hat.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'

EXEC sys.sp_addextendedproperty @level1name=N'UsageStatisticDownload', @level2name=N'UserId', @value=N'ApplicationUser.Id', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'UsageStatisticDownload', @level2name=N'Usage', @value=N'Json-Repräsentation der summarischen Benutzer Usage-Statistik', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
