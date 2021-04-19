
/* ---------------------------------------------------------------------- */
/* Add table "dbo.OrderExecutedWaitList"                                  */
/* ---------------------------------------------------------------------- */

GO


CREATE TABLE [dbo].[OrderExecutedWaitList] (
    [OrderExecutedWaitListId] INTEGER IDENTITY(1,1) NOT NULL,
    [VeId] INTEGER NOT NULL,
    [Processed] BIT CONSTRAINT [DEF_OrderExecutedWaitList_Processed] DEFAULT 0 NOT NULL,
    [ProcessedDate] DATETIME2,
    [SerializedMessage] NVARCHAR(max) NOT NULL,
    [InsertedOn] DATETIME2 CONSTRAINT [DEF_OrderExecutedWaitList_InsertedOn] DEFAULT getdate() NOT NULL,
    CONSTRAINT [PK_OrderExecutedWaitList] PRIMARY KEY CLUSTERED ([OrderExecutedWaitListId])
)
GO


CREATE NONCLUSTERED INDEX [IDX_OrderExecutedWaitList_1] ON [dbo].[OrderExecutedWaitList] ([VeId])
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Tabelle enthält diejenigen VE''s die durch einen Digitalisierungsauftrag digitalisiert wurden und bei denen Vecteur gemeldet hat, dass der Auftrag erledigt ist. Wenn das AIS aber noch nicht aktualisiert wurde, kann die Benachrichtigung noch nicht abgeschlossen werden. Daher werden diese VEs hier gespeichert.  Am Ende einer Synchronisierung wird geschaut, ob die VE in dieser Tabelle vorhanden ist und ggf. der DigitalisierungsAuftragErledigt Event auf den Bus gelegt.', 'SCHEMA', N'dbo', 'TABLE', N'OrderExecutedWaitList', NULL, NULL
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Eindeutiger Primärschlüssel', 'SCHEMA', N'dbo', 'TABLE', N'OrderExecutedWaitList', 'COLUMN', N'OrderExecutedWaitListId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Verweis auf die Verzeichnungseinheit', 'SCHEMA', N'dbo', 'TABLE', N'OrderExecutedWaitList', 'COLUMN', N'VeId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Ja/Nein ob diese VE bereits abgearbeitet wurde', 'SCHEMA', N'dbo', 'TABLE', N'OrderExecutedWaitList', 'COLUMN', N'Processed'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Datum an welchem die Verarbeitung stattgefunden hat.', 'SCHEMA', N'dbo', 'TABLE', N'OrderExecutedWaitList', 'COLUMN', N'ProcessedDate'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Die Serialisierte Nachricht der Originalnachricht. Wird benötigt um die Nachricht nochmals auslösen zu können.', 'SCHEMA', N'dbo', 'TABLE', N'OrderExecutedWaitList', 'COLUMN', N'SerializedMessage'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Datum an welchem der Datensatz eingefügt wurde', 'SCHEMA', N'dbo', 'TABLE', N'OrderExecutedWaitList', 'COLUMN', N'InsertedOn'
GO

