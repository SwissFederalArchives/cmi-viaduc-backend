
-- Anpassungen für Spalte ReasonForRejectionDate 

ALTER TABLE ApplicationUser DROP CONSTRAINT DF_ApplicationUser_ReasonForRejectionDate;
GO

ALTER TABLE ApplicationUser DROP COLUMN ReasonForRejectionDate
GO

ALTER TABLE ApplicationUser ADD ReasonForRejectionDate DATETIME NULL
GO
