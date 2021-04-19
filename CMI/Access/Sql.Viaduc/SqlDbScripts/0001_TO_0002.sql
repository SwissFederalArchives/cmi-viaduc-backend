

CREATE TABLE [dbo].[Reason] (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	CONSTRAINT [PK_Reason] PRIMARY KEY CLUSTERED 
	(
		[ID] ASC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]


INSERT INTO [dbo].[Reason] ([Name]) VALUES ('als Beweismittel'), ('für Gesetzgebung oder Rechtsprechung'), ('für statistische Zwecke'), ('für einen Entscheid betr. einem Einsichts-/Auskunftsgesuch')

CREATE TABLE [dbo].[Order] (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](200) NOT NULL,
	[Type] [int] NOT NULL,
	[Status] [int] NOT NULL,
	CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED 
	(
		[ID] ASC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

CREATE TABLE [dbo].[OrderItem] (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OrderId_FK] INT NOT NULL,
	[Ve] [int] NOT NULL,
	[Reason_FK] INT,
	CONSTRAINT [PK_Orderitem] PRIMARY KEY CLUSTERED 
	(
		[ID] ASC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]


ALTER TABLE [dbo].[OrderItem]  WITH CHECK ADD  CONSTRAINT [FK_OrderItem_Order] FOREIGN KEY([OrderId_FK])
REFERENCES [dbo].[Order] ([ID])

ALTER TABLE [dbo].[OrderItem] CHECK CONSTRAINT [FK_OrderItem_Order]
