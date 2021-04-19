/* ---------------------------------------------------------------------- */
/* Add table "PrimaerdatenAuftrag"                                        */
/* ---------------------------------------------------------------------- */

GO


CREATE TABLE [PrimaerdatenAuftrag] (
    [PrimaerdatenAuftragId] INTEGER IDENTITY(1,1) NOT NULL,
    [AufbereitungsArt] NVARCHAR(50) NOT NULL,
    [GroesseInBytes] INTEGER,
    [Verarbeitungskanal] INTEGER,
    [Status] NVARCHAR(100) NOT NULL,
    [Service] NVARCHAR(100) NOT NULL,
    [PackageId] NVARCHAR(250) NOT NULL,
    [PackageMetadata] NVARCHAR(max),
    [VeId] INTEGER NOT NULL,
    [Abgeschlossen] BIT CONSTRAINT [DEF_PrimaerdatenAuftrag_Abgeschlossen] DEFAULT 0 NOT NULL,
    [AbgeschlossenAm] DATETIME2,
	[GeschaetzteAufbereitungszeit] INT,
    [ErrorText] NVARCHAR(max),
    [Workload] NVARCHAR(max),
    [CreatedOn] DATETIME2 CONSTRAINT [DEF_PrimaerdatenAuftrag_CreatedOn] DEFAULT getdate() NOT NULL,
    [ModifiedOn] DATETIME2,
    CONSTRAINT [PK_PrimaerdatenAuftrag] PRIMARY KEY CLUSTERED ([PrimaerdatenAuftragId])
)
GO


CREATE NONCLUSTERED INDEX [IX_PrimaerdatenAuftrag_1] ON [PrimaerdatenAuftrag] ([Status] ASC)
GO


CREATE NONCLUSTERED INDEX [IX_PrimaerdatenAuftrag_2] ON [PrimaerdatenAuftrag] ([Verarbeitungskanal] ASC,[AufbereitungsArt] ASC)
GO


CREATE NONCLUSTERED INDEX [IX_PrimaerdatenAuftrag_3] ON [PrimaerdatenAuftrag] ([Abgeschlossen] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_PrimaerdatenAuftrag_4] ON [PrimaerdatenAuftrag] ([Abgeschlossen] ASC, [VeId] ASC, [Status] ASC)
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Eindeutiger Primärschlüssel', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'PrimaerdatenAuftragId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Die Art des Auftrags. Sync oder Download', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'AufbereitungsArt'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Die Grösse des Pakets gemäss Metadaten', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'GroesseInBytes'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Hält fest in welchem Kanaltyp die Verarbeitung stattfindet. Es sind 4 Kanäle vorgesehen. Je nach Grösse wird das Paket in einen dieser Kanäle zugeteilt.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'Verarbeitungskanal'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Der aktuelle Verarbeitungsstatus des Pakets.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'Status'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'In welchem Service der Auftrag aktuell in Arbeit ist.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'Service'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Die PackageId des Pakets welche aus dem DIR geholt werden soll.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'PackageId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Die PackageMetadaten wie diese aus dem DIR geholt worden sind als serialisiertes JSON Objekt.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'PackageMetadata'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Die ID der Verzeichnungseinheit', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'VeId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Gibt an ob der Auftrag abgeschlossen ist oder nicht.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'Abgeschlossen'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Datum an welchem der Auftrag als abgeschlossen markiert wurde.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'AbgeschlossenAm'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Tritt ein Verarbeitungsfehler auf, wird hier die Fehlermeldung festgehalten.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'ErrorText'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'JSON Serialisiertes Objekt welche die Daten des Auftrags enthält. Bei einer Synchronisation ist dies der ArchiveRecord, der bereits extrahiert wurde. Beim Download die Informationen zum Download.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'Workload'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Zeitpunkt wann der Datensatz erstellt worden ist.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'CreatedOn'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Zeitpunkt der letzten Modifikation. Korrespondiert in der Regel mit dem Status.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'ModifiedOn'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Die geschätzte Aufbereitungszeit in Sekunden', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'GeschaetzteAufbereitungszeit'
GO


/* ---------------------------------------------------------------------- */
/* Add table "PrimaerdatenAuftragLog"                                     */
/* ---------------------------------------------------------------------- */

GO


CREATE TABLE [PrimaerdatenAuftragLog] (
    [PrimaerdatenAuftragLogId] INTEGER IDENTITY(1,1) NOT NULL,
    [PrimaerdatenAuftragId] INTEGER NOT NULL,
    [Status] NVARCHAR(100) NOT NULL,
    [Service] NVARCHAR(100) NOT NULL,
    [CreatedOn] DATETIME2 CONSTRAINT [DEF_PrimaerdatenAuftragLog_CreatedOn] DEFAULT getdate() NOT NULL,
    [ErrorText] NVARCHAR(max),
    CONSTRAINT [PK_PrimaerdatenAuftragLog] PRIMARY KEY CLUSTERED ([PrimaerdatenAuftragLogId])
)
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Eindeutiger Primärschlüssel', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftragLog', 'COLUMN', N'PrimaerdatenAuftragLogId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Primarykey des übergeordneten Auftrags', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftragLog', 'COLUMN', N'PrimaerdatenAuftragId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Der Status', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftragLog', 'COLUMN', N'Status'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Der Service welcher die Statusänderung verursacht hat.', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftragLog', 'COLUMN', N'Service'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Zeitpunkt der Erstellung des Datensatzes', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftragLog', 'COLUMN', N'CreatedOn'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Allfälliger Fehlertext', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftragLog', 'COLUMN', N'ErrorText'
GO


/* ---------------------------------------------------------------------- */
/* Add foreign key constraints                                            */
/* ---------------------------------------------------------------------- */

GO


ALTER TABLE [PrimaerdatenAuftragLog] ADD CONSTRAINT [PrimaerdatenAuftrag_PrimaerdatenAuftragLog] 
    FOREIGN KEY ([PrimaerdatenAuftragId]) REFERENCES [PrimaerdatenAuftrag] ([PrimaerdatenAuftragId]) ON DELETE CASCADE
GO
