/* Neue Spalten hinzufügen */
ALTER TABLE dbo.OrderItem ADD
	DatumDerFreigabe DATETIME2,
	SachbearbeiterId NVARCHAR(200)
GO

EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'DatumDerFreigabe', @value=N'Gibt an, wann der Auftrag (!= E) freigegeben oder zurückgewiesen wurde.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'SachbearbeiterId', @value=N'Gibt an, welcher Sachbearbeiter den Auftrag/Gesuch freigegeben bzw. entschieden hat.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

ALTER TABLE dbo.OrderItem ADD CONSTRAINT CK_OrderItem_EntwederEntscheidOderFreigabe 
CHECK (EntscheidGesuch = 0 OR ApproveStatus = 0)
GO

DROP TABLE ApproveStatusHistory;

INSERT INTO [dbo].[ApplicationUser]
           ([ID]
           ,[FamilyName]
           ,[FirstName]
           ,[Organization]
           ,[Street]
           ,[StreetAttachment]
           ,[ZipCode]
           ,[Town]
           ,[CountryCode]
           ,[EmailAddress]
           ,[PhoneNumber]
           ,[SkypeName]
           ,[Setting]
           ,[Claims]
           ,[Created]
           ,[Updated]
           ,[UserExtId]
           ,[Language]
           ,[CreatedOn]
           ,[CreatedBy]
           ,[ModifiedOn]
           ,[ModifiedBy]
           ,[Birthday]
           ,[FabasoftDossier]
           ,[ReasonForRejection]
           ,[IsInternalUser]
           ,[RolePublicClient]
           ,[EiamRoles]
           ,[ResearcherGroup]
           ,[BarInternalConsultation]
           ,[IdentifierDocument]
           ,[MobileNumber]
           ,[ReasonForRejectionDate]
           ,[DownloadLimitDisabledUntil])
     VALUES
           ('MigrationUser'
           ,'User'
           ,'Migration'
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , GETDATE()
           , GETDATE()
           , null
           ,'de'
           ,GETDATE()
           ,'DbUpgrade'
           ,GETDATE()
           ,'DbUpgrade'
           ,null
           ,null
           ,null
           ,1
           ,null
           ,null
           ,0
           ,0
           ,null
           ,null
           ,null
           ,null)
GO

INSERT INTO [dbo].[ApplicationUser]
           ([ID]
           ,[FamilyName]
           ,[FirstName]
           ,[Organization]
           ,[Street]
           ,[StreetAttachment]
           ,[ZipCode]
           ,[Town]
           ,[CountryCode]
           ,[EmailAddress]
           ,[PhoneNumber]
           ,[SkypeName]
           ,[Setting]
           ,[Claims]
           ,[Created]
           ,[Updated]
           ,[UserExtId]
           ,[Language]
           ,[CreatedOn]
           ,[CreatedBy]
           ,[ModifiedOn]
           ,[ModifiedBy]
           ,[Birthday]
           ,[FabasoftDossier]
           ,[ReasonForRejection]
           ,[IsInternalUser]
           ,[RolePublicClient]
           ,[EiamRoles]
           ,[ResearcherGroup]
           ,[BarInternalConsultation]
           ,[IdentifierDocument]
           ,[MobileNumber]
           ,[ReasonForRejectionDate]
           ,[DownloadLimitDisabledUntil])
     VALUES
           ('System'
           ,'System'
           ,''
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , GETDATE()
           , GETDATE()
           , null
           ,'de'
           ,GETDATE()
           ,'DbUpgrade'
           ,GETDATE()
           ,'DbUpgrade'
           ,null
           ,null
           ,null
           ,1
           ,null
           ,null
           ,0
           ,0
           ,null
           ,null
           ,null
           ,null)
GO


INSERT INTO [dbo].[ApplicationUser]
           ([ID]
           ,[FamilyName]
           ,[FirstName]
           ,[Organization]
           ,[Street]
           ,[StreetAttachment]
           ,[ZipCode]
           ,[Town]
           ,[CountryCode]
           ,[EmailAddress]
           ,[PhoneNumber]
           ,[SkypeName]
           ,[Setting]
           ,[Claims]
           ,[Created]
           ,[Updated]
           ,[UserExtId]
           ,[Language]
           ,[CreatedOn]
           ,[CreatedBy]
           ,[ModifiedOn]
           ,[ModifiedBy]
           ,[Birthday]
           ,[FabasoftDossier]
           ,[ReasonForRejection]
           ,[IsInternalUser]
           ,[RolePublicClient]
           ,[EiamRoles]
           ,[ResearcherGroup]
           ,[BarInternalConsultation]
           ,[IdentifierDocument]
           ,[MobileNumber]
           ,[ReasonForRejectionDate]
           ,[DownloadLimitDisabledUntil])
     VALUES
           ('Vecteur'
           ,'Vecteur'
           ,''
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , null
           , GETDATE()
           , GETDATE()
           , null
           ,'de'
           ,GETDATE()
           ,'DbUpgrade'
           ,GETDATE()
           ,'DbUpgrade'
           ,null
           ,null
           ,null
           ,1
           ,null
           ,null
           ,0
           ,0
           ,null
           ,null
           ,null
           ,null)
GO

UPDATE OrderItem SET SachbearbeiterId = 'MigrationUser' WHERE ApproveStatus > 0 OR EntscheidGesuch > 0
UPDATE OrderItem SET DatumDerFreigabe = GETDATE() WHERE ApproveStatus > 0

ALTER TABLE [dbo].[OrderItem]  WITH CHECK ADD CONSTRAINT [FK_OrderItem_Sachbearbeiter_User] FOREIGN KEY(SachbearbeiterId)
REFERENCES [dbo].[ApplicationUser] ([ID])

drop view v_UserOverview;
GO
CREATE VIEW v_UserOverview AS
	SELECT 
		appu.Id,
		appu.UserExtId,
		appu.Created,
		appu.CreatedOn,
		appu.CreatedBy,
		appu.ModifiedOn,
		appu.ModifiedBy,
		appu.FamilyName,
		appu.FirstName,
		appu.Organization,
		appu.Street,
		appu.StreetAttachment,
		appu.ZipCode,
		appu.Town,
		appu.CountryCode,
		appu.PhoneNumber,
		appu.MobileNumber,
		appu.Birthday,
		appu.EmailAddress,
		appu.FabasoftDossier,
		appu.Language,
		appu.RolePublicClient,
		appu.EiamRoles,
		appu.ReasonForRejection,
		appu.ResearcherGroup,
		appu.BarInternalConsultation,
		appu.IsInternalUser,
		appu.DownloadLimitDisabledUntil,
		CASE WHEN appu.IdentifierDocument IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Identifizierungsmittel,
		
		AblieferndeStellenId = STUFF((SELECT ', ' + CONVERT(VARCHAR(max), AblieferndeStelleId)
				FROM ApplicationUserAblieferndeStelle
				WHERE UserId = appu.Id
				GROUP BY AblieferndeStelleId
				FOR XML PATH(''), TYPE).value('.','NVARCHAR(max)'), 1, 1, ''),

		AblieferndeStellenKuerzel = STUFF((SELECT ', ' + Kuerzel
				FROM ApplicationUserAblieferndeStelle
					INNER JOIN AblieferndeStelle ON AblieferndeStelle.AblieferndeStelleId = ApplicationUserAblieferndeStelle.AblieferndeStelleId  
				WHERE UserId = appu.Id
				GROUP BY AblieferndeStelle.Kuerzel
				FOR XML PATH(''), TYPE).value('.','NVARCHAR(max)'), 1, 1, ''),

		ApplicationUserRollesId = STUFF((SELECT ', ' + CONVERT(VARCHAR(max), RoleId)
				FROM ApplicationRoleUser
				WHERE UserId = appu.Id
				GROUP BY RoleId
				FOR XML PATH(''), TYPE).value('.','NVARCHAR(max)'), 1, 1, ''),
		
		ApplicationUserRolles = STUFF((SELECT ', ' + Name
				FROM ApplicationRoleUser
					INNER JOIN ApplicationRole ON ApplicationRole.Id = ApplicationRoleUser.RoleId  
				WHERE UserId = appu.Id
				GROUP BY Name
				FOR XML PATH(''), TYPE).value('.','NVARCHAR(max)'), 1, 1, ''),
		
		AblieferndeStellenTokenId = STUFF((SELECT ', ' + CONVERT(VARCHAR(max), AsTokenMapping.TokenId)
				FROM ApplicationUserAblieferndeStelle
					INNER JOIN AsTokenMapping ON AsTokenMapping.AblieferndeStelleId = ApplicationUserAblieferndeStelle.AblieferndeStelleId
				WHERE UserId = appu.Id
				GROUP BY AsTokenMapping.TokenId
				FOR XML PATH(''), TYPE).value('.','NVARCHAR(max)'), 1, 1, ''),

		AblieferndeStellenToken = STUFF((SELECT ', ' + Token
				FROM ApplicationUserAblieferndeStelle
					INNER JOIN AsTokenMapping ON AsTokenMapping.AblieferndeStelleId = ApplicationUserAblieferndeStelle.AblieferndeStelleId
					INNER JOIN AblieferndeStelleToken ON AblieferndeStelleToken.TokenId = AsTokenMapping.TokenId 
				WHERE UserId = appu.Id
				GROUP BY Token
				FOR XML PATH(''), TYPE).value('.','NVARCHAR(max)'), 1, 1, '')
	
	from 
		ApplicationUser appu
	where 
		appu.Id <> 'System' AND appu.Id <> 'Vecteur' AND appu.Id <> 'MigrationUser'
GO