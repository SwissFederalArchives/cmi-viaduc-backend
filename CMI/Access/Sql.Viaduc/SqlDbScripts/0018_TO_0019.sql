ALTER TABLE [dbo].[ApplicationUser] ADD [Language] NVARCHAR(2) NOT NULL DEFAULT 'de'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Sprache, in welcher der Benutzer EMails empfangen möchte.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationUser', @level2type=N'COLUMN',@level2name=N'Language'
