

CREATE TABLE [dbo].[ApplicationUser](
	[ID] [nvarchar](200) NOT NULL,
	[FamilyName] [nvarchar](200) NULL,
	[FirstName] [nvarchar](200) NULL,
	[Organization] [nvarchar](200) NULL,
	[Street] [nvarchar](200) NULL,
	[StreetAttachment] [nvarchar](200) NULL,
	[ZipCode] [nvarchar](200) NULL,
	[Town] [nvarchar](200) NULL,
	[CountryCode] [nvarchar](10) NULL,
	[EmailAddress] [nvarchar](200) NULL,
	[PhoneNumber] [nvarchar](200) NULL,
	[SkypeName] [nvarchar](200) NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]




