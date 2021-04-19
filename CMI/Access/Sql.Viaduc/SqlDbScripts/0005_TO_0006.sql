CREATE TABLE [dbo].[UserUsageStatistics](
	[UserId] [nvarchar](200) NOT NULL,
	[Usage] NVARCHAR(MAX) NULL,
 CONSTRAINT [PK_UserUsageStatistics] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[UserUsageStatistics]  WITH CHECK ADD  CONSTRAINT [FK_UserUsageStatistics_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[ApplicationUser] ([ID])

ALTER TABLE [dbo].[UserUsageStatistics] CHECK CONSTRAINT [FK_UserUsageStatistics_User]

EXEC sys.sp_addextendedproperty @level1name=N'UserUsageStatistics', @value=N'Enthält die summarischen Usage Statistiken der Benutzer', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'

EXEC sys.sp_addextendedproperty @level1name=N'UserUsageStatistics', @level2name=N'UserId', @value=N'ApplicationUser.Id', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'UserUsageStatistics', @level2name=N'Usage', @value=N'Json-Repräsentation der summarischen Benutzer Usage-Statistik', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

