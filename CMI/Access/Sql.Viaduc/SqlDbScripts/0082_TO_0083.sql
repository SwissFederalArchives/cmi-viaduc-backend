/* ---------------------------------------------------------------------- */
/* Add table "dbo.Collection"                                             */
/* ---------------------------------------------------------------------- */
GO

CREATE TABLE [Collection] (
    [CollectionId] INTEGER IDENTITY(1,1) NOT NULL,
    [ParentId] INTEGER,
    [Language] NVARCHAR(2) DEFAULT 'de' NOT NULL,
    [Title] NVARCHAR(255) NOT NULL,
    [DescriptionShort] NVARCHAR(512) NOT NULL,
    [Description] NVARCHAR(max) NOT NULL,
    [ValidFrom] DATETIME2 DEFAULT getdate() NOT NULL,
    [ValidTo] DATETIME2 NOT NULL,
    [CollectionTypeId] INTEGER NOT NULL,
    [Image] VARBINARY(max),
    [Thumbnail] VARBINARY(max),
    [ImageAltText] NVARCHAR(255),
    [ImageMimeType] NVARCHAR(255),
    [Link] NVARCHAR(4000),
    [CollectionPath] NVARCHAR(400) NOT NULL,
    [SortOrder] INTEGER DEFAULT 0 NOT NULL,
    [CreatedOn] DATETIME2 CONSTRAINT [DEF_Collection_CreatedOn] DEFAULT getdate() NOT NULL,
    [CreatedBy] NVARCHAR(255) NOT NULL,
    [ModifiedOn] DATETIME2,
    [ModifiedBy] NVARCHAR(255),
    CONSTRAINT [PK_Collection] PRIMARY KEY CLUSTERED ([CollectionId])
)
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Primarykey ', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'CollectionId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Points to the parent collection', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'ParentId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'The language as ISO code de, en, fr', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'Language'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Title of the collection', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'Title'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'A short description of the collection', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'DescriptionShort'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'A full description of the collection in markup language', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'Description'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'From which point in time the collection should be visible to the users', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'ValidFrom'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Until which point in time the collection should be visible to the users', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'ValidTo'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Indicates the type of collections: Themenblöcke and Ausstellungen (Topics and Collections)', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'CollectionTypeId'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'An image that gives a hint to the collection', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'Image'
GO

EXECUTE sp_addextendedproperty N'MS_Description', N'The thumbnail to the image', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'Thumbnail'
GO

EXECUTE sp_addextendedproperty N'MS_Description', N'The accessibility help information for the image', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'ImageAltText'
GO

EXECUTE sp_addextendedproperty N'MS_Description', N'The mime type of the image', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'ImageMimeType'
GO

EXECUTE sp_addextendedproperty N'MS_Description', N'The URL that will lead the user to the collection details', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'Link'
GO

EXECUTE sp_addextendedproperty N'MS_Description', N'The path of the item consisting of a padded string of the ids from first parent to current item.', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'CollectionPath'
GO

EXECUTE sp_addextendedproperty N'MS_Description', N'Allows to order the collections in a specific way', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'SortOrder'
GO

EXECUTE sp_addextendedproperty N'MS_Description', N'Date when record was added', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'CreatedOn'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'User that added the record', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'CreatedBy'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'Date when the record was modified', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'ModifiedOn'
GO


EXECUTE sp_addextendedproperty N'MS_Description', N'User that modified the record', 'SCHEMA', N'dbo', 'TABLE', N'Collection', 'COLUMN', N'ModifiedBy'
GO


/* ---------------------------------------------------------------------- */
/* Add foreign key constraints                                            */
/* ---------------------------------------------------------------------- */
GO

ALTER TABLE [dbo].[Collection] ADD CONSTRAINT [Collection_Collection] 
    FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Collection] ([CollectionId])
GO


/* ---------------------------------------------------------------------- */
/* Add views                                                              */
/* ---------------------------------------------------------------------- */
GO
CREATE VIEW v_CollectionList
AS
SELECT c.CollectionId,
       c.ParentId,
       c.Language,
       c.Title,
       c.DescriptionShort,
       c.Description,
       c.ValidFrom,
       c.ValidTo,
       c.CollectionTypeId,
       c.ImageAltText,
       c.ImageMimeType,
       c.Link,
       c.CollectionPath,
       c.SortOrder,
       c.CreatedOn,
       c.CreatedBy,
       c.ModifiedOn,
       c.ModifiedBy,
       pc.Title AS Parent,
       Stuff ((SELECT ' | ' + cc.title
               FROM   collection cc
               WHERE  cc.parentid = c.collectionid
               GROUP  BY cc.collectionid,
                         cc.title,
                         cc.sortorder
               ORDER  BY cc.sortorder
               FOR xml path(''), type).value('.', 'nvarchar(max)'), 1, 2, '') AS ChildCollections
FROM   dbo.collection c
       LEFT JOIN dbo.collection pc
              ON c.parentid = pc.collectionid; 
GO
