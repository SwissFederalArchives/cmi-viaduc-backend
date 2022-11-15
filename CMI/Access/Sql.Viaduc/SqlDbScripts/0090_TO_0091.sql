ALTER TABLE [ApplicationUser] 
DROP CONSTRAINT IF EXISTS CHK_ROLEPUBLICCLIENT_VS_ISINTERNALUSER
GO

ALTER TABLE [ApplicationUser] 
ALTER COLUMN [CreatedBy] nvarchar(500) NULL;
GO

ALTER TABLE [ApplicationUser] 
ALTER COLUMN [ModifiedBy] nvarchar(500) NULL;
GO

ALTER TABLE [ApplicationUser] 
ALTER COLUMN [FabasoftDossier] nvarchar(1000) NULL;
GO

ALTER TABLE [ApplicationUser] 
ALTER COLUMN [ReasonForRejection] nvarchar(1000) NULL;
GO

ALTER TABLE [ApplicationUser] 
ALTER COLUMN [RolePublicClient] nvarchar(3) NULL;
GO

ALTER TABLE [ApplicationUser] 
ALTER COLUMN [EiamRoles] nvarchar(10) NULL;
GO

UPDATE ApplicationUser SET RolePublicClient = 'BAR' WHERE RolePublicClient IS NULL OR RolePublicClient = ''
UPDATE ApplicationUser SET IsInternalUser = CASE WHEN RolePublicClient IN ('BAR', 'AS', 'BVW') THEN 1 ELSE 0 END

ALTER TABLE ApplicationUser
ADD CONSTRAINT CHK_ROLEPUBLICCLIENT_VS_ISINTERNALUSER CHECK ((IsInternalUser = 1 AND RolePublicClient IN ('BAR', 'AS', 'BVW')) OR (IsInternalUser = 0 AND RolePublicClient IN ('Ö2', 'Ö3')))
GO

ALTER TABLE [AblieferndeStelle] 
ALTER COLUMN [Bezeichnung] nvarchar(255) NOT NULL;

ALTER TABLE [AblieferndeStelle] 
ALTER COLUMN [Kuerzel] nvarchar(255) NULL;
GO

ALTER TABLE [AblieferndeStelle] 
ALTER COLUMN [ModifiedBy] nvarchar(500) NULL;
GO

ALTER TABLE [AblieferndeStelle] 
ALTER COLUMN [CreatedBy] nvarchar(500) NULL;
GO

ALTER TABLE [AblieferndeStelleToken]
ALTER COLUMN [Bezeichnung] nvarchar(255) NOT NULL;
GO

ALTER TABLE [Favorite]
ALTER COLUMN [Title] nvarchar(max) NULL;
GO

ALTER TABLE [Favorite]
ALTER COLUMN [URL] nvarchar(max) NULL;
GO

ALTER TABLE [News]
ALTER COLUMN [DE] nvarchar(max) NOT NULL;
GO

ALTER TABLE [News]
ALTER COLUMN [En] nvarchar(max)  NOT NULL;
GO

ALTER TABLE [News]
ALTER COLUMN [Fr] nvarchar(max)  NOT NULL;
GO

ALTER TABLE [News]
ALTER COLUMN [It] nvarchar(max)  NOT NULL;
GO

ALTER TABLE [News]
ALTER COLUMN [DEHeader] nvarchar(max)  NOT NULL;
GO

ALTER TABLE [News]
ALTER COLUMN [EnHeader] nvarchar(max)  NOT NULL;
GO

ALTER TABLE [News]
ALTER COLUMN [FrHeader] nvarchar(max)  NOT NULL;
GO

ALTER TABLE [News]
ALTER COLUMN [ItHeader] nvarchar(max)  NOT NULL;
GO

ALTER TABLE [UserRoleHistory]
ALTER COLUMN [FromPrimaryRole] nvarchar(3)  NOT NULL;
GO

ALTER TABLE [UserRoleHistory]
ALTER COLUMN [ToPrimaryRole] nvarchar(3)  NOT NULL;
GO
