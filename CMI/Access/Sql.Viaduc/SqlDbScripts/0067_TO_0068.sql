
-- Spalte hinzufügen
ALTER TABLE OrderItem ADD GebrauchskopieStatus INT NOT NULL DEFAULT 0

-- Berschreibung hinzufügen 
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'GebrauchskopieStatus', @value=N'Enthält den Status der Gebrauchskopie zur Benutzungskopie', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'

