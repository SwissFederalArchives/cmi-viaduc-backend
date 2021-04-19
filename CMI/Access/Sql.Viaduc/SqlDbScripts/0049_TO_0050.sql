-- SuperUser, RechercheManager, PublicationManager, HistoricalAnalyst, MetadataManager löschen werden nicht verwendet
DELETE FROM ApplicationRoleUser WHERE RoleId IN (1000001, 1000003, 1000005, 1000006, 1000010)
DELETE FROM ApplicationRoleFeature WHERE RoleId IN (1000001, 1000003, 1000005, 1000006, 1000010)
GO
DELETE FROM ApplicationRole WHERE Id IN (1000003, 1000005, 1000006, 1000010)
GO

UPDATE ApplicationRole SET Identifier = 'Standard', Name = 'Standard', Description = 'Standard minimale Rechte; wird dem ALLOW Benutzer automatisch zugewiesen' WHERE Id = 1000001
GO
