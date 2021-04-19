
-- *** Beschreibungen für Tabellen hinzufügen ***
-- Mögliches Vorgehen zum Erstellen einer Beschreibung: 
-- Eine der folgenden Codezeilen kopieren und die Parameter auf den beschriebenen Wert setzen:
-- @level1name=N'Table', @value=N'Beschreibung'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @value=N'Enthält Benutzer, die sich an Viaduc angemeldet haben', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'Favorite', @value=N'Enthält Favoriten, die auf eine VE verweissen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'FavoriteList', @value=N'Zum Gruppieren der Favoriten', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'Version', @value=N'Nachdem eine neue Programmversion von Viaduc eingesetzt wird, kann Viaduc die Datenbank automatisch an die neue Programmversion anpassen. Mit dieser Tabelle ermittelt das Programm, ob eine Anpassung notwendig ist.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @name=N'MS_Description'


-- *** Beschreibungen für Spalten hinzufügen ***
-- Mögliches Vorgehen zum Erstellen einer Beschreibung: 
-- Eine der folgenden Codezeilen kopieren und die Parameter auf den beschriebenen Wert setzen:
-- @level1name=N'Table', @level2name=N'Column', @value=N'Beschreibung'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'ID', @value=N'Vom eIAM Token Claim /identity/claims/nameidentifier', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'FamilyName', @value=N'Vom eIAM Token Claim /identity/claims/surname', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'FirstName', @value=N'Vom eIAM Token Claim /identity/claims/givenname', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'Organization', @value=N'Der Benutzer kann diese Information selber erfassen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'Street', @value=N'Der Benutzer kann diese Information selber erfassen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'StreetAttachment', @value=N'Der Benutzer kann diese Information selber erfassen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'ZipCode', @value=N'Der Benutzer kann diese Information selber erfassen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'Town', @value=N'Der Benutzer kann diese Information selber erfassen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'CountryCode', @value=N'Der Benutzer kann diese Information selber erfassen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'EmailAddress', @value=N'Vom eIAM Token Claim /identity/claims/emailaddress', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'PhoneNumber', @value=N'Der Benutzer kann diese Information selber erfassen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'SkypeName', @value=N'Der Benutzer kann diese Information selber erfassen', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

EXEC sys.sp_addextendedproperty @level1name=N'Favorite', @level2name=N'List', @value=N'Verweist auf FavoriteList.ID', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

EXEC sys.sp_addextendedproperty @level1name=N'FavoriteList', @level2name=N'Name', @value=N'Dieser Text wird dem Benutzer angezeigt', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

