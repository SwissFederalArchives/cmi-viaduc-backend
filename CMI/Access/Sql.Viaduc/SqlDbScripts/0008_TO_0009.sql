-- ***** Script File für Application und User Role  *****

-- Spalten zu [dbo].[ApplicationUser] hinzufügen
ALTER TABLE [dbo].[ApplicationUser] ADD [Claims] NVARCHAR(MAX) NULL
ALTER TABLE [dbo].[ApplicationUser] ADD [Tokens] NVARCHAR(900) NULL
ALTER TABLE [dbo].[ApplicationUser] ADD [Created] DATETIME NOT NULL CONSTRAINT [DF_ApplicationUser_Created]  DEFAULT (getdate())
ALTER TABLE [dbo].[ApplicationUser] ADD [Updated] DATETIME NOT NULL CONSTRAINT [DF_ApplicationUser_Updated]  DEFAULT (getdate())

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Claims (json) von der letzten Anmeldung' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationUser', @level2type=N'COLUMN',@level2name=N'Claims'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Access Tokens (comma-separated List) von der letzten Anmeldung' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationUser', @level2type=N'COLUMN',@level2name=N'Tokens'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Erstellungsdatum' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationUser', @level2type=N'COLUMN',@level2name=N'Created'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Änderungsdatum' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationUser', @level2type=N'COLUMN',@level2name=N'Updated'



ALTER TABLE [dbo].[ApplicationUser] ADD [Fulltext] AS (CONCAT([EmailAddress],' ',[FirstName],' ',[FamilyName],' ',[Street],' ',[ZipCode],' ',[Town],' ',[PhoneNumber],' ',[SkypeName],' ',[ID]))

CREATE NONCLUSTERED INDEX [IX_ApplicationUser_Fulltext] ON [dbo].[ApplicationUser]([Fulltext] ASC) ON [PRIMARY]

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Volltext' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationUser', @level2type=N'COLUMN',@level2name=N'Fulltext'



-- Administrator-Benutzer hinzufügen
DECLARE @AdministratorID NVARCHAR(200) = 'Administrator';

INSERT INTO [dbo].[ApplicationUser] ([ID]) VALUES (@AdministratorID);


-- Erstellen [dbo].[ApplicationFeature]
CREATE TABLE [dbo].[ApplicationFeature](
	[ID] [int] IDENTITY(1000000,1) NOT NULL,
	[Identifier] [nvarchar](100) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Created] [datetime] NOT NULL CONSTRAINT [DF_ApplicationFeature_Created]  DEFAULT (getdate()),
	[Updated] [datetime] NOT NULL CONSTRAINT [DF_ApplicationFeature_Updated]  DEFAULT (getdate()),
	CONSTRAINT [PK_ApplicationFeature] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [IX_ApplicationFeature_Identifier] ON [dbo].[ApplicationFeature]( [ID] ASC ) ON [PRIMARY]


EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Technische ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationFeature', @level2type=N'COLUMN',@level2name=N'ID'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Identifier' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationFeature', @level2type=N'COLUMN',@level2name=N'Identifier'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name (de)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationFeature', @level2type=N'COLUMN',@level2name=N'Name'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Beschreibung (de) optional' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationFeature', @level2type=N'COLUMN',@level2name=N'Description'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Erstellungsdatum' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationFeature', @level2type=N'COLUMN',@level2name=N'Created'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Änderungsdatum' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationFeature', @level2type=N'COLUMN',@level2name=N'Updated'


-- Befüllen [dbo].[ApplicationFeature]

SET IDENTITY_INSERT [dbo].[ApplicationFeature] ON 

INSERT [dbo].[ApplicationFeature] ([ID], [Identifier], [Name], [Description]) VALUES (1000000, N'ApplicationRoleManagement', N'Verwaltung der feingranularen Rollen', NULL)

SET IDENTITY_INSERT [dbo].[ApplicationFeature] OFF


-- Erstellen [dbo].[ApplicationRole]
CREATE TABLE [dbo].[ApplicationRole](
	[ID] [int] IDENTITY(1000000,1) NOT NULL,
	[Identifier] [nvarchar](100) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Created] [datetime] NOT NULL CONSTRAINT [DF_ApplicationRole_Created]  DEFAULT (getdate()),
	[Updated] [datetime] NOT NULL CONSTRAINT [DF_ApplicationRole_Updated]  DEFAULT (getdate()),
	CONSTRAINT [PK_ApplicationRole] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [IX_ApplicationRole_Identifier] ON [dbo].[ApplicationRole]( [ID] ASC ) ON [PRIMARY]

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Technische ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRole', @level2type=N'COLUMN',@level2name=N'ID'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Identifier' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRole', @level2type=N'COLUMN',@level2name=N'Identifier'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Name (de)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRole', @level2type=N'COLUMN',@level2name=N'Name'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Beschreibung (de) optional' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRole', @level2type=N'COLUMN',@level2name=N'Description'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Erstellungsdatum' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRole', @level2type=N'COLUMN',@level2name=N'Created'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Änderungsdatum' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRole', @level2type=N'COLUMN',@level2name=N'Updated'

-- Befüllen [dbo].[ApplicationRole]

SET IDENTITY_INSERT [dbo].[ApplicationRole] ON 

INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000000, N'ApplicationOwner', N'Application Owner', N'Application Owners verwalten die Basis-Einstellungen und -Konfigurationen des Management.')
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000001, N'SuperUser', N'Super User', NULL)
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000002, N'UserManager', N'Benutzer-Manager', NULL)
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000003, N'RechercheManager', N'Rechere-Manager', NULL)
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000004, N'AccessManager', N'Freigabe-Manager', NULL)
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000005, N'PublicationManager', N'Publikations-Manager', NULL)
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000006, N'HistoricalAnalyst', N'Historie-Analyst', NULL)
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000007, N'UGCManager', N'UGC Manager', NULL)
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000008, N'OrderManager', N'Lesesaal-/Bestell-Manager', NULL)
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000009, N'LogicsticsManager', N'Logistiker', NULL)
INSERT [dbo].[ApplicationRole] ([ID], [Identifier], [Name], [Description]) VALUES (1000010, N'MetadataManager', N'Metadaten-Manager', NULL)

SET IDENTITY_INSERT [dbo].[ApplicationRole] OFF


-- Erstellen [dbo].[ApplicationRoleFeature]
CREATE TABLE [dbo].[ApplicationRoleFeature](
	[ID] [int] IDENTITY(1000000,1) NOT NULL,
	[RoleId] [int] NOT NULL,
	[FeatureId] [int] NOT NULL,
	[InsertedByUserId] [nvarchar](200) NOT NULL,
	[Inserted] [datetime] NOT NULL CONSTRAINT [DF_ApplicationRoleFeature_Inserted]  DEFAULT (getdate()),
	CONSTRAINT [PK_ApplicationRoleFeature] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[ApplicationRoleFeature]  WITH CHECK ADD  CONSTRAINT [FK_ApplicationRoleFeature_Role] FOREIGN KEY([RoleId])
REFERENCES [dbo].[ApplicationRole] ([ID])
ALTER TABLE [dbo].[ApplicationRoleFeature] CHECK CONSTRAINT [FK_ApplicationRoleFeature_Role]

ALTER TABLE [dbo].[ApplicationRoleFeature]  WITH CHECK ADD  CONSTRAINT [FK_ApplicationRoleFeature_ApplicationFeature] FOREIGN KEY([FeatureId])
REFERENCES [dbo].[ApplicationFeature] ([ID])
ALTER TABLE [dbo].[ApplicationRoleFeature] CHECK CONSTRAINT [FK_ApplicationRoleFeature_ApplicationFeature]

ALTER TABLE [dbo].[ApplicationRoleFeature]  WITH CHECK ADD  CONSTRAINT [FK_ApplicationRoleFeature_ApplicationUser_InsertedByUserId] FOREIGN KEY([InsertedByUserId])
REFERENCES [dbo].[ApplicationUser] ([ID])
ALTER TABLE [dbo].[ApplicationRoleFeature] CHECK CONSTRAINT [FK_ApplicationRoleFeature_ApplicationUser_InsertedByUserId]


EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Technische ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleFeature', @level2type=N'COLUMN',@level2name=N'ID'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'ApplicationRole.ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleFeature', @level2type=N'COLUMN',@level2name=N'RoleId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'ApplicationFeature.ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleFeature', @level2type=N'COLUMN',@level2name=N'FeatureId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'ApplicationUser.ID des Benutzers, der diese Benutzer-Rolle eingefügt hat' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleFeature', @level2type=N'COLUMN',@level2name=N'InsertedByUserId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Einfügedatum' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleFeature', @level2type=N'COLUMN',@level2name=N'Inserted'

-- Befüllen [dbo].[ApplicationRoleFeature]
INSERT INTO [dbo].[ApplicationRoleFeature] ([RoleId], [FeatureId], [InsertedByUserId]) VALUES (1000000, 1000000, @AdministratorID);


-- Erstellen [dbo].[ApplicationRoleUser]
CREATE TABLE [dbo].[ApplicationRoleUser](
	[ID] [int] IDENTITY(1000000,1) NOT NULL,
	[RoleId] [int] NOT NULL,
	[UserId] [nvarchar](200) NOT NULL,
	[InsertedByUserId] [nvarchar](200) NOT NULL,
	[Inserted] [datetime] NOT NULL CONSTRAINT [DF_ApplicationRoleUser_Inserted]  DEFAULT (getdate()),
	CONSTRAINT [PK_ApplicationRoleUser] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]


ALTER TABLE [dbo].[ApplicationRoleUser]  WITH CHECK ADD  CONSTRAINT [FK_ApplicationRoleUser_ApplicationUser] FOREIGN KEY([UserId])
REFERENCES [dbo].[ApplicationUser] ([ID])

ALTER TABLE [dbo].[ApplicationRoleUser] CHECK CONSTRAINT [FK_ApplicationRoleUser_ApplicationUser]

ALTER TABLE [dbo].[ApplicationRoleUser]  WITH CHECK ADD  CONSTRAINT [FK_ApplicationRoleUser_ApplicationUser_InsertedByUserId] FOREIGN KEY([InsertedByUserId])
REFERENCES [dbo].[ApplicationUser] ([ID])

ALTER TABLE [dbo].[ApplicationRoleUser] CHECK CONSTRAINT [FK_ApplicationRoleUser_ApplicationUser_InsertedByUserId]

ALTER TABLE [dbo].[ApplicationRoleUser]  WITH CHECK ADD  CONSTRAINT [FK_ApplicationRoleUser_Role] FOREIGN KEY([RoleId])
REFERENCES [dbo].[ApplicationRole] ([ID])

ALTER TABLE [dbo].[ApplicationRoleUser] CHECK CONSTRAINT [FK_ApplicationRoleUser_Role]


EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Technische ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleUser', @level2type=N'COLUMN',@level2name=N'ID'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'ApplicationRole.ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleUser', @level2type=N'COLUMN',@level2name=N'RoleId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'ApplicationUser.ID' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleUser', @level2type=N'COLUMN',@level2name=N'UserId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'ApplicationUser.ID des Benutzers, der diese Benutzer-Rolle eingefügt hat' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleUser', @level2type=N'COLUMN',@level2name=N'InsertedByUserId'
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Einfügedatum' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ApplicationRoleUser', @level2type=N'COLUMN',@level2name=N'Inserted'

