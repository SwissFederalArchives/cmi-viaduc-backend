
-- *** Tabelle Reason mehrsprachig ***

ALTER TABLE [dbo].[Reason] DROP COLUMN [Name]

ALTER TABLE [dbo].[Reason] ADD [Name_de] NVARCHAR(255) NULL
ALTER TABLE [dbo].[Reason] ADD [Name_fr] NVARCHAR(255) NULL
ALTER TABLE [dbo].[Reason] ADD [Name_it] NVARCHAR(255) NULL
ALTER TABLE [dbo].[Reason] ADD [Name_en] NVARCHAR(255) NULL
GO

UPDATE [dbo].[Reason] SET [Name_de] = 'als Beweismittel', [Name_fr] = '(!fr) als Beweismittel', [Name_it] =  '(!it) als Beweismittel', [Name_en] = '(!en) als Beweismittel' WHERE ID = 1
UPDATE [dbo].[Reason] SET [Name_de] = 'für Gesetzgebung oder Rechtsprechung', [Name_fr] = '(!fr) für Gesetzgebung oder Rechtsprechung', [Name_it] =  '(!it) für Gesetzgebung oder Rechtsprechung', [Name_en] =  '(!en) für Gesetzgebung oder Rechtsprechung' WHERE ID = 2
UPDATE [dbo].[Reason] SET [Name_de] = 'für statistische Zwecke', [Name_fr] = '(!fr) für statistische Zwecke', [Name_it] =  '(!it) für statistische Zwecke', [Name_en] =  '(!en) für statistische Zwecke' WHERE ID = 3
UPDATE [dbo].[Reason] SET [Name_de] = 'für einen Entscheid betr. einem Einsichts-/Auskunftsgesuch', [Name_fr] = '(!fr) für einen Entscheid betr. einem Einsichts-/Auskunftsgesuch', [Name_it] =  '(!it) für einen Entscheid betr. einem Einsichts-/Auskunftsgesuch', [Name_en] =  '(!en) für einen Entscheid betr. einem Einsichts-/Auskunftsgesuch' WHERE ID = 4
GO

ALTER TABLE [dbo].[Reason] ALTER COLUMN [Name_de] NVARCHAR(255) NOT NULL
ALTER TABLE [dbo].[Reason] ALTER COLUMN [Name_fr] NVARCHAR(255) NOT NULL
ALTER TABLE [dbo].[Reason] ALTER COLUMN [Name_it] NVARCHAR(255) NOT NULL
ALTER TABLE [dbo].[Reason] ALTER COLUMN [Name_en] NVARCHAR(255) NOT NULL
GO
