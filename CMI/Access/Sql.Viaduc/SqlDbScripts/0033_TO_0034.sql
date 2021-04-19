/* ---------------------------------------------------------------------- */
/* Drop falsly named table and recreate correctly                         */
/* ---------------------------------------------------------------------- */
GO

DROP TABLE [dbo].[TempMigrationWorkspace2]
GO

CREATE TABLE [dbo].[TempMigrationWorkspace] (
    [Email] NVARCHAR(200) NOT NULL,
    [Quelle] NVARCHAR(100) NOT NULL,
    [Liste] NVARCHAR(300) NOT NULL,
    [Kommentar] NVARCHAR(2000),
    [Verzeichnungseinheit_Id] INTEGER NOT NULL
)
GO

CREATE INDEX IX_TempMigrationWorkspace_Email on TempMigrationWorkspace (Email)
GO

EXECUTE sp_addextendedproperty N'MS_Description', N'Temporäre Tabelle für die Migration der Arbeitsmappen aus swiss-archives', 'SCHEMA', N'dbo', 'TABLE', N'TempMigrationWorkspace', NULL, NULL
GO

