-- ***** Script File für ApplicationFeature entfernen  *****

ALTER TABLE [dbo].[ApplicationRoleFeature] ADD [Feature_tmp] NVARCHAR(900) NULL;
GO


PRINT 'ApplicationFeature entfernen: FeatureId migrieren';

DECLARE @sql NVARCHAR(MAX);
SET @sql = '
	WITH roleFeatures AS (
		SELECT arf.ID AS roleFeatureID, af.Identifier AS featureIdentifier FROM ApplicationRoleFeature arf INNER JOIN ApplicationFeature af ON arf.FeatureID = af.ID
	)
	UPDATE
		[dbo].[ApplicationRoleFeature]
	SET
		Feature_tmp = featureIdentifier
	FROM
		roleFeatures
	WHERE
		(ID = roleFeatures.roleFeatureID)
	;
';

EXEC sp_executesql @sql;

BEGIN TRY  
	ALTER TABLE [dbo].[ApplicationRoleFeature] DROP CONSTRAINT FK_ApplicationRoleFeature_ApplicationFeature;
END TRY  
BEGIN CATCH  
END CATCH
	
ALTER TABLE [dbo].[ApplicationRoleFeature] ALTER COLUMN FeatureId NVARCHAR(200) NOT NULL;

CREATE NONCLUSTERED INDEX [IX_ApplicationRoleFeature_FeatureId] ON [dbo].[ApplicationRoleFeature]([FeatureId] ASC) ON [PRIMARY];

SET @sql = '
	UPDATE
		[dbo].[ApplicationRoleFeature]
	SET
		FeatureId = Feature_tmp
';

EXEC sp_executesql @sql;

SET @sql = 'ALTER TABLE [dbo].[ApplicationRoleFeature] DROP COLUMN Feature_tmp';

EXEC sp_executesql @sql;

EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=N'Feature-Identifier' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleFeature', @level2type=N'COLUMN',@level2name=N'FeatureId';

GO


PRINT 'ApplicationFeature entfernen: Tabelle ApplicationFeature löschen';

DROP TABLE [dbo].[ApplicationFeature]

GO