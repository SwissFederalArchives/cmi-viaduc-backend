/* ---------------------------------------------------------------------- */
/* Add column to table "PrimaerdatenAuftrag"                              */
/* ---------------------------------------------------------------------- */

GO


ALTER TABLE [PrimaerdatenAuftrag] ADD 
    [PriorisierungsKategorie] INTEGER
GO



EXECUTE sp_addextendedproperty N'MS_Description', N'Die Priorisierungskategorie die sich über die Grösse und der Auftragsquelle definiert', 'SCHEMA', N'dbo', 'TABLE', N'PrimaerdatenAuftrag', 'COLUMN', N'PriorisierungsKategorie'
GO

