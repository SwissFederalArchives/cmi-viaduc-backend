/* CREATE DOWNLOAD REASON HISTORY TABLE  */
CREATE TABLE [dbo].[DownloadReasonHistory] (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](200) NOT NULL,
	[DownloadedAt] DATETIME NOT NULL,
	[ReasonId] [int] NOT NULL,
	[VeId] [int] NOT NULL,
	CONSTRAINT [PK_DownloadReasonHistory] PRIMARY KEY CLUSTERED 
	(
		[ID] ASC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

/* Constraint DownloadReasonHistory <-> ApplicationUser  */
ALTER TABLE [dbo].[DownloadReasonHistory]  WITH CHECK ADD CONSTRAINT [FK_DownloadReasonHistory_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[Applicationuser] ([ID])
ALTER TABLE [dbo].[DownloadReasonHistory] CHECK CONSTRAINT [FK_DownloadReasonHistory_User]

/* Constraint DownloadReasonHistory <-> Reason  */
ALTER TABLE [dbo].[DownloadReasonHistory]  WITH CHECK ADD CONSTRAINT [FK_DownloadReasonHistory_Reason] FOREIGN KEY([ReasonId])
REFERENCES [dbo].[Reason] ([ID])
ALTER TABLE [dbo].[DownloadReasonHistory] CHECK CONSTRAINT [FK_DownloadReasonHistory_Reason]

EXEC sys.sp_addextendedproperty @level1name=N'DownloadReasonHistory', @level2name=N'UserId', @value=N'Enthält die Referenz zum User, der den Download ausgeführt hat', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'DownloadReasonHistory', @level2name=N'DownloadedAt', @value=N'Enthält das Downloaddatum', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'DownloadReasonHistory', @level2name=N'ReasonId', @value=N'Enthält die Referenz zum Downloadgrund', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'DownloadReasonHistory', @level2name=N'VeId', @value=N'Enthält die Referenz zur betroffenen Verzeichniseinheit', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

/* Add UserId to DownloadTokenHistory  */
ALTER TABLE DownloadToken
ADD UserId NVARCHAR(200) NOT NULL

EXEC sys.sp_addextendedproperty @level1name=N'DownloadToken', @level2name=N'UserId', @value=N'Enthält die Referenz zum User, der den Download ausgeführt hat', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'