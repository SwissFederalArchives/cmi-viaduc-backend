-- Spalten hinzufügen
ALTER TABLE [dbo].[OrderItem] ADD [BewilligungsDatum] DATETIME NULL

-- Beschreibung hinzufügen für Spalten
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'BewilligungsDatum', @value=N'Bewilligungsdatum, welches der Benutzer eingeben kann, wenn er eine Bewilligung besitzt', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
