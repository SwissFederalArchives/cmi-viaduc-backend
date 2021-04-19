
/* ---------------------------------------------------------------------- */
/* Alter table "dbo.FavoriteList"                                         */
/* ---------------------------------------------------------------------- */
ALTER TABLE [dbo].[FavoriteList] ADD
    [Comment] NVARCHAR(2000)

ALTER TABLE [dbo].[FavoriteList] ALTER COLUMN [Name] NVARCHAR(300) NOT NULL


/* ---------------------------------------------------------------------- */
/* Add table "dbo.TempMigrationWorkspace"                                 */
/* ---------------------------------------------------------------------- */

CREATE TABLE [dbo].[TempMigrationWorkspace2] (
    [Email] NVARCHAR(200) NOT NULL,
    [Quelle] NVARCHAR(100) NOT NULL,
    [Liste] NVARCHAR(300) NOT NULL,
    [Kommentar] NVARCHAR(2000),
    [Verzeichnungseinheit_Id] INTEGER NOT NULL
)
