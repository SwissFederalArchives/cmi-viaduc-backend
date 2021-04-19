
-- *** Tabelle AblieferndeStelle ergänzen ***

ALTER TABLE AblieferndeStelle ADD
    Kontrollstellen nvarchar(max),
	CreatedOn datetime NOT NULL CONSTRAINT DF_AblieferndeStelle_CreatedOn DEFAULT sysdatetime(),
	CreatedBy varchar(500) NULL,
	ModifiedOn datetime NOT NULL CONSTRAINT DF_AblieferndeStelle_ModifiedOn DEFAULT sysdatetime(),
	ModifiedBy varchar(500) NULL
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Das Feld enthält 0 bis n Mailadressen', @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'AblieferndeStelle', @level2type=N'COLUMN', @level2name=N'Kontrollstellen'
GO
