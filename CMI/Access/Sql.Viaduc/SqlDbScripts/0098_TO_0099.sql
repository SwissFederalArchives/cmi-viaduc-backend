/* ---------------------------------------------------------------------- */
/* Drop and add table "SyncAction"                                                 */
/* ---------------------------------------------------------------------- */

IF OBJECT_ID(N'dbo.[SyncActionLog]', N'U') IS NOT NULL  
   DROP TABLE [dbo].[SyncActionLog];   
GO

IF OBJECT_ID(N'dbo.[SyncAction]', N'U') IS NOT NULL  
   DROP TABLE [dbo].[SyncAction]  
GO

CREATE TABLE [SyncAction] (
    [SyncActionId] BIGINT IDENTITY(1,1) NOT NULL,
    [ArchiveRecordId] NVARCHAR(255),
    [ActionType] NVARCHAR(40),
    [ActionStatus] INTEGER DEFAULT 0,
    [NumberOfTries] INTEGER DEFAULT 0,
    [CreatedOn] DATETIME2 DEFAULT getdate(),
    [ModifiedOn] DATETIME2 DEFAULT getdate(),
    CONSTRAINT [PK_SyncAction] PRIMARY KEY CLUSTERED ([SyncActionId])
)
 
GO


CREATE NONCLUSTERED INDEX [IDX_SyncAction_1] ON [SyncAction] ([ArchiveRecordId] ASC,[ActionStatus] ASC,[ActionType] ASC)
GO


CREATE NONCLUSTERED INDEX [IDX_SyncAction_2] ON [SyncAction] ([ArchiveRecordId] ASC,[ActionStatus] ASC)
GO


CREATE NONCLUSTERED INDEX [IDX_SyncAction_3] ON [SyncAction] ([ActionStatus] ASC)
GO


CREATE NONCLUSTERED INDEX [IDX_SyncAction_4] ON [SyncAction] ([ModifiedOn] ASC,[ActionStatus] ASC)
GO


CREATE NONCLUSTERED INDEX [IDX_SyncAction_5] ON [SyncAction] ([ModifiedOn] ASC)
GO


/* ---------------------------------------------------------------------- */
/* Add table "SyncActionLog"                                              */
/* ---------------------------------------------------------------------- */

GO

CREATE TABLE [SyncActionLog] (
    [SyncActionLogId] INTEGER IDENTITY(1,1) NOT NULL,
    [SyncActionId] BIGINT,
    [LogDate] DATETIME2 DEFAULT getdate(),
    [ActionStatusHistory] NVARCHAR(40),
    [ErrorReason] NVARCHAR(max),
    CONSTRAINT [PK_SyncActionLog] PRIMARY KEY CLUSTERED ([SyncActionLogId])
)
 
GO


CREATE NONCLUSTERED INDEX [IDX_SyncActionLog_1] ON [SyncActionLog] ([LogDate] ASC,[ActionStatusHistory] ASC)
GO


/* ---------------------------------------------------------------------- */
/* Add foreign key constraints                                            */
/* ---------------------------------------------------------------------- */

GO


ALTER TABLE [SyncActionLog] ADD CONSTRAINT [SyncAction_SyncActionLog] 
    FOREIGN KEY ([SyncActionId]) REFERENCES [SyncAction] ([SyncActionId]) ON DELETE CASCADE
GO


CREATE TABLE [SyncInfo](
	[SyncInfoId] [bigint] IDENTITY(1,1) NOT NULL,
	[LastChangeDate] [datetime2] NULL,
CONSTRAINT [PK_SyncInfo] PRIMARY KEY CLUSTERED
(
	[SyncInfoId] ASC
))
GO
