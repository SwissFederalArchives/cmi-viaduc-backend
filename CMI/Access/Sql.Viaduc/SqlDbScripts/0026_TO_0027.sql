-- *** Für Tabelle OrderItem ***

-- Spalten hinzufügen
ALTER TABLE OrderItem ADD DigitalisierungsKategorie INT NULL
ALTER TABLE OrderItem ADD TerminDigitalisierung DATETIME NULL
ALTER TABLE OrderItem ADD InternalComment NVARCHAR(MAX) NULL
GO

CREATE INDEX IX_OrderItem_Status ON OrderItem([Status]);
GO

-- Dokumentation
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'DigitalisierungsKategorie', @value=N'Für Digitalisierungsaufträge; Wird verwendet zur Ermittlung welcher Digitalisierungsauftrag als nächstes ausgeführt wird', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
EXEC sys.sp_addextendedproperty @level1name=N'OrderItem', @level2name=N'TerminDigitalisierung', @value=N'Für Digitalisierungsaufträge; Wird verwendet zur Ermittlung welcher Digitalisierungsauftrag als nächstes ausgeführt wird', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level2type=N'COLUMN', @name=N'MS_Description'
GO