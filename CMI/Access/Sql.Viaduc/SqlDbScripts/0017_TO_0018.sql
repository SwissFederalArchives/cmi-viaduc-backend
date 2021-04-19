-- ***** Script File für Abliefernde-Stelle-Tokens  *****

ALTER TABLE [dbo].[ApplicationUser] ADD [AsTokens] NVARCHAR(900) NULL

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Abliefernde-Stelle Tokens (comma-separated List) von der letzten Anmeldung' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationUser', @level2type=N'COLUMN',@level2name=N'AsTokens'
