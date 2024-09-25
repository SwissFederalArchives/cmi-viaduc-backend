--------------------------------------------------------------------------------------------------
-- Zugang AS: Umbenennen von IsInternalUser auf IsIdentifiedUser und hinzufügen neuer Anmeldewerte
--------------------------------------------------------------------------------------------------

-- Alten Constraint entfernen
ALTER TABLE [ApplicationUser] DROP CONSTRAINT [CHK_ROLEPUBLICCLIENT_VS_ISINTERNALUSER]
GO

-- Tabelle ApplicationUser ändern
EXECUTE sp_rename N'ApplicationUser.IsInternalUser', N'IsIdentifiedUser', 'COLUMN' 
GO

ALTER TABLE [ApplicationUser] 
ADD [QoAValue] INT NULL;
GO

ALTER TABLE [ApplicationUser] 
ADD [HomeName] nvarchar(200) NULL;
GO

ALTER TABLE [ApplicationUser] 
ADD [LastLoginDate] datetime NULL;
GO

-- Daten aktualisieren. Aktuell sind alle Internen Benutzer auch Identifizierte Benutzer und haben deshalb mindestens den QoA 40
-- Aktuelle Ö2 Benutzer sind sicher CH-Logins und erhalten den QoA-Wert 20
-- Aktuelle Ö3 Benutzer sind sicher CH-Logins und erhalten den QoA-Wert 30
Update [ApplicationUser] Set QoAValue = 40, HomeName = 'E-ID FED-LOGIN' where IsIdentifiedUser = 1
GO
Update [ApplicationUser] Set QoAValue = 20, HomeName = 'E-ID CH-LOGIN' where IsIdentifiedUser = 0 and RolePublicClient = 'Ö2'
GO
Update [ApplicationUser] Set QoAValue = 30, HomeName = 'E-ID CH-LOGIN' where IsIdentifiedUser = 0 and RolePublicClient = 'Ö3'
GO
Update  [ApplicationUser] Set RolePublicClient = 'BAR' where ID = 'VIEWER'
GO


-- Den neuen Check-Constraint erstellen.
ALTER TABLE [ApplicationUser]  WITH CHECK ADD  CONSTRAINT [CHK_ROLEPUBLICCLIENT_VS_ISIDETIFIEDUSER] CHECK  
(([IsIdentifiedUser]=(1) AND [RolePublicClient] IN ('BVW','AS','BAR', 'Ö3') AND [QoAValue] >= 40)
  OR ([IsIdentifiedUser] = 0 AND [QoAValue] < 40))
GO

ALTER TABLE [ApplicationUser] CHECK CONSTRAINT [CHK_ROLEPUBLICCLIENT_VS_ISIDETIFIEDUSER]
GO


ALTER VIEW [dbo].[v_UserOverview] AS
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
		appu.IsIdentifiedUser,
		appu.DownloadLimitDisabledUntil,
		appu.DigitalisierungsbeschraenkungAufgehobenBis,
		appu.QoAValue,
		appu.HomeName,
		appu.LastLoginDate,
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
		appu.Id <> 'System' AND appu.Id <> 'Vecteur' AND appu.Id <> 'MigrationUser' AND appu.Id <> 'Viewer'
GO


