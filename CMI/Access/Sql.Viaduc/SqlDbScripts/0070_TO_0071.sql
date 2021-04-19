
ALTER TABLE ApplicationUser
  ADD DigitalisierungsbeschraenkungAufgehobenBis datetime DEFAULT NULL;
GO

EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'DigitalisierungsbeschraenkungAufgehobenBis', @value=N'Gibt an, bis wann beim Benutzer die Digitalisierungsbeschränkung aufgehoben wird.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

DROP VIEW v_UserOverview;
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
		appu.DigitalisierungsbeschraenkungAufgehobenBis,
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