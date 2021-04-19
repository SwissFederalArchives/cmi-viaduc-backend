
-- ***** Script File für Bestellungen  *****

-- Spalten hinzufügen
ALTER TABLE [dbo].[OrderItem] ADD [Comment] NVARCHAR(MAX) NULL
ALTER TABLE [dbo].[OrderItem] ADD [Status] INT NOT NULL
ALTER TABLE [dbo].[Order] ADD [OrderDate] DATETIME NULL

ALTER TABLE [dbo].[Order] DROP COLUMN [Status]
ALTER TABLE [dbo].[Order] ALTER COLUMN Type INT NULL

-- Umbenennen (Tabelle und Spalten)
EXEC sp_rename 'dbo.Order', 'Ordering' -- Grund: Order ist ein Reserved Keyword in SQL
EXEC sp_rename 'dbo.OrderItem.OrderId_FK', 'OrderId', 'COLUMN'
EXEC sp_rename 'dbo.OrderItem.Reason_FK', 'Reason', 'COLUMN'

-- Berschreibung hinzufügen für Spalten
EXEC sys.sp_addextendedproperty @level1name=N'Ordering', @level2name=N'Type', @value=N'Liefertyp zum z.B. Digitalisat oder Lesesaalausleihe', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'Comment', @value=N'Freitext der vom Benutzer erfasst werden kann', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'Status', @value=N'Interner Status der Bestellposition', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'Reason', @value=N'Begründung gemäss Art. 14 BGA', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
