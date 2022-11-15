IF NOT EXISTS (
    SELECT * 
    FROM   sys.columns 
    WHERE  object_id = OBJECT_ID(N'[dbo].[ApplicationUser]') 
            AND name = 'ActiveAspNetSessionId')
    BEGIN
        ALTER TABLE ApplicationUser
            ADD ActiveAspNetSessionId NVARCHAR(MAX) DEFAULT '';
        EXEC sys.sp_addextendedproperty @level1name=N'ApplicationUser', @level2name=N'ActiveAspNetSessionId', @value=N'Enthält die ASP-Session ID falls der Benutzer nicht abgemeldet ist.', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
    END
GO