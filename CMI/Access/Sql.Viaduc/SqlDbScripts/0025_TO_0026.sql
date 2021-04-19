/* ---------------------------------------------------------------------- */
/* Alter table "dbo.OrderItem"                                            */
/* ---------------------------------------------------------------------- */

ALTER TABLE [dbo].[OrderItem] ADD
    [ApproveStatus] INTEGER
GO



EXECUTE sp_addextendedproperty N'MS_Description', N'Der aktuelle Freigabestatus', 'SCHEMA', N'dbo', 'TABLE', N'OrderItem', 'COLUMN', N'ApproveStatus'
GO


/* ---------------------------------------------------------------------- */
/* Add table "dbo.StatusHistory"                                          */
/* ---------------------------------------------------------------------- */

GO


CREATE TABLE [dbo].[StatusHistory] (
    [ID] INTEGER IDENTITY(1,1) NOT NULL,
    [OrderItemId] INTEGER NOT NULL,
    [StatusChangeDate] DATETIME2 CONSTRAINT [DEF_StatusHistory_StatusChangeDate] DEFAULT getdate() NOT NULL,
    [FromStatus] INTEGER NOT NULL,
    [ToStatus] INTEGER NOT NULL,
    [ChangedBy] NVARCHAR(200) NOT NULL,
    CONSTRAINT [PK_StatusHistory] PRIMARY KEY ([ID])
)
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Enthält die Statusübergänge einer Bestellung', 'SCHEMA', N'dbo', 'TABLE', N'StatusHistory', NULL, NULL
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Fremdschlüssel zu OrderItem', 'SCHEMA', N'dbo', 'TABLE', N'StatusHistory', 'COLUMN', N'OrderItemId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Zeitstempel wenn die Statusänderung stattgefunden hat', 'SCHEMA', N'dbo', 'TABLE', N'StatusHistory', 'COLUMN', N'StatusChangeDate'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Status der die Bestellposition vor der Änderung hatte.', 'SCHEMA', N'dbo', 'TABLE', N'StatusHistory', 'COLUMN', N'FromStatus'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Status der Bestellposition nach der Änderung.', 'SCHEMA', N'dbo', 'TABLE', N'StatusHistory', 'COLUMN', N'ToStatus'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Benutzer der die Änderung vorgenommen hat, oder "System" wenn das System die Änderung automatisch vorgenommen hat.', 'SCHEMA', N'dbo', 'TABLE', N'StatusHistory', 'COLUMN', N'ChangedBy'
GO


/* ---------------------------------------------------------------------- */
/* Add table "dbo.ApproveStatusHistory"                                   */
/* ---------------------------------------------------------------------- */

GO


CREATE TABLE [dbo].[ApproveStatusHistory] (
    [ID] INTEGER IDENTITY(1,1) NOT NULL,
    [OrderItemId] INTEGER NOT NULL,
    [OrderType] INTEGER NOT NULL,
    [ApprovedTo] NVARCHAR(max),
    [ApproveFromStatus] INTEGER,
    [ApproveToStatus] INTEGER NOT NULL,
    [ApprovalDate] DATETIME2 CONSTRAINT [DEF_ApproveStatusHistory_ApprovalDate] DEFAULT getdate() NOT NULL,
	[ApprovedFrom] NVARCHAR(200)
    CONSTRAINT [PK_ApproveStatusHistory] PRIMARY KEY CLUSTERED ([ID])
)
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Hält fest welche Freigabestatus-Änderungen stattgefunden haben.', 'SCHEMA', N'dbo', 'TABLE', N'ApproveStatusHistory', NULL, NULL
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Fremdschlüssel der Bestellposition', 'SCHEMA', N'dbo', 'TABLE', N'ApproveStatusHistory', 'COLUMN', N'OrderItemId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Art der Bestellung. Müsste in der Regel dem Type der Bestellung entsprechen.', 'SCHEMA', N'dbo', 'TABLE', N'ApproveStatusHistory', 'COLUMN', N'OrderType'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Name, Vorname, Organisation des bewilligten Benutzers', 'SCHEMA', N'dbo', 'TABLE', N'ApproveStatusHistory', 'COLUMN', N'ApprovedTo'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Der alte Freigabestatus', 'SCHEMA', N'dbo', 'TABLE', N'ApproveStatusHistory', 'COLUMN', N'ApproveFromStatus'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Der gewählte Freigabestatus', 'SCHEMA', N'dbo', 'TABLE', N'ApproveStatusHistory', 'COLUMN', N'ApproveToStatus'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Datum der Änderung', 'SCHEMA', N'dbo', 'TABLE', N'ApproveStatusHistory', 'COLUMN', N'ApprovalDate'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Wer die Änderung veranlasste', 'SCHEMA', N'dbo', 'TABLE', N'ApproveStatusHistory', 'COLUMN', N'ApprovedFrom'
GO

/* ---------------------------------------------------------------------- */
/* Add foreign key constraints                                            */
/* ---------------------------------------------------------------------- */

GO

ALTER TABLE [dbo].[StatusHistory] ADD CONSTRAINT [OrderItem_StatusHistory] 
    FOREIGN KEY ([OrderItemId]) REFERENCES [dbo].[OrderItem] ([ID])
GO


ALTER TABLE [dbo].[ApproveStatusHistory] ADD CONSTRAINT [OrderItem_ApproveStatusHistory] 
    FOREIGN KEY ([OrderItemId]) REFERENCES [dbo].[OrderItem] ([ID])
GO

