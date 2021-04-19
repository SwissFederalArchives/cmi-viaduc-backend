IF OBJECT_ID('dbo.[CHK_ROLEPUBLICCLIENT_VS_ISINTERNALUSER]') IS NOT NULL ALTER TABLE ApplicationUser DROP CONSTRAINT CHK_ROLEPUBLICCLIENT_VS_ISINTERNALUSER
GO

UPDATE ApplicationUser SET RolePublicClient = (SELECT SUBSTRING(Tokens, LEN(Tokens) - CHARINDEX(',', REVERSE(Tokens)) +2, LEN(Tokens)))
UPDATE ApplicationUser SET RolePublicClient = 'Ö2' WHERE RolePublicClient IS NULL OR RolePublicClient = ''
--UPDATE ApplicationUser SET EiamRoles = 'APPO' 
UPDATE ApplicationUser SET IsInternalUser = CASE WHEN RolePublicClient IN ('BAR', 'AS', 'BVW') THEN 1 ELSE 0 END

ALTER TABLE ApplicationUser ADD CONSTRAINT CHK_ROLEPUBLICCLIENT_VS_ISINTERNALUSER CHECK ((IsInternalUser = 1 AND RolePublicClient IN ('BAR', 'AS', 'BVW')) OR (IsInternalUser = 0 AND RolePublicClient IN ('Ö2', 'Ö3')))
GO
