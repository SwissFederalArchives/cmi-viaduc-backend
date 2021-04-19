ALTER TABLE ApplicationUser ADD CreatedOn DATETIME NOT NULL CONSTRAINT DF_ApplicationUser_CreatedOn DEFAULT(SYSDATETIME())
ALTER TABLE ApplicationUser ADD CreatedBy VARCHAR(500) NULL
ALTER TABLE ApplicationUser ADD ModifiedOn DATETIME NOT NULL CONSTRAINT DF_ApplicationUser_ModifiedOn DEFAULT(SYSDATETIME()) 
ALTER TABLE ApplicationUser ADD ModifiedBy VARCHAR(500) NULL

ALTER TABLE ApplicationUser ADD Birthday DATETIME NULL
ALTER TABLE ApplicationUser ADD FabasoftDossier VARCHAR(1000) NULL
ALTER TABLE ApplicationUser ADD ReasonForRejection VARCHAR(100) NULL

ALTER TABLE ApplicationUser ADD IsInternalUser BIT NOT NULL DEFAULT 1
ALTER TABLE ApplicationUser ADD RolePublicClient VARCHAR(3) NULL
ALTER TABLE ApplicationUser ADD EiamRoles VARCHAR(10) NULL

ALTER TABLE ApplicationUser ADD ResearcherGroup BIT NOT NULL DEFAULT 0
ALTER TABLE ApplicationUser ADD BarInternalConsultation BIT NOT NULL DEFAULT 0

GO

ALTER TABLE ApplicationUser ADD CONSTRAINT CHK_ROLEPUBLICCLIENT_VS_ISINTERNALUSER CHECK ((IsInternalUser = 1 AND RolePublicClient IN ('BAR', 'AS', 'BVW')) OR (IsInternalUser = 0 AND RolePublicClient IN ('OE2', 'OE3')))

--Neue Tabelle für die Historisierung der Benutzerrollen
CREATE TABLE [dbo].[UserRoleHistory](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ApplicationUserId] [nvarchar](200) NOT NULL,
	[PrimaryRoleChangeDate] [datetime2](7) NOT NULL,
	[FromPrimaryRole] [varchar](3) NOT NULL,
	[ToPrimaryRole]  [varchar](3) NOT NULL,
	[ChangedBy] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_UserRoleHistory] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


ALTER TABLE [dbo].[UserRoleHistory]  WITH CHECK ADD  CONSTRAINT [ApplicationUser_UserRoleHistory] FOREIGN KEY([ApplicationUserId])
REFERENCES [dbo].[ApplicationUser] ([ID])
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
	
	FROM 
		ApplicationUser appu
GO
