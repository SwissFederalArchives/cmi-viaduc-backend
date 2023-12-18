CREATE OR ALTER View v_Primaerdatenaufbereitung
AS
SELECT    
	ofi.OrderingDate, 
	ofi.OrderingType, 
	ofi.ItemId as OrderItemId, 
	ofi.Dossiertitel,
	PrimaerdatenAuftrag.VeId,
	ofi.DigitalisierungsKategorie as DigitalisierungsKategorieId,
	ofi.Signatur,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 1) AS NeuEingegangen,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 9) AS Ausgeliehen,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 12) AS ZumReponierenBereit,
	(Select max(CreatedOn) from PrimaerdatenAuftragLog where PrimaerdatenAuftragId = PrimaerdatenAuftrag.PrimaerdatenAuftragId and Status = 'AuftragErledigt') AS AuftragErledigt,
	CASE PrimaerdatenAuftrag.AufbereitungsArt 
	WHEN 'Sync' THEN
		    (Select min(AbgeschlossenAm) from PrimaerdatenAuftrag pda where VeId = PrimaerdatenAuftrag.VeId and pda.createdOn > PrimaerdatenAuftrag.CreatedOn and pda.AufbereitungsArt = 'Download')
	ELSE 
		    (Select min(AbgeschlossenAm) from PrimaerdatenAuftrag pda where VeId = PrimaerdatenAuftrag.VeId and pda.PrimaerdatenAuftragId = PrimaerdatenAuftrag.PrimaerdatenAuftragId and pda.AufbereitungsArt = 'Download')
	END as Abgeschlossen,
	PrimaerdatenAuftrag.PrimaerdatenAuftragId,
	PrimaerdatenAuftrag.AufbereitungsArt, 
	PrimaerdatenAuftrag.GroesseInBytes,
	PrimaerdatenAuftrag.GeschaetzteAufbereitungszeit,
	CASE 
		WHEN Charindex('"FileCount":', packagemetadata) > 0 THEN 
		Substring(packagemetadata, Charindex('"FileCount":', packagemetadata) + Len('"FileCount":'),
		Charindex(',"PackageId":', packagemetadata) - Charindex('"FileCount":', packagemetadata) - Len('"FileCount":'))
		ELSE 0
		END AS AnzahlDateien
 FROM PrimaerdatenAuftrag
  INNER JOIN v_OrderingFlatItem AS ofi
    ON PrimaerdatenAuftrag.VeId = ofi.VeId
			AND ofi.OrderId = (Select min(j.OrderId)
                                       from v_OrderingFlatItem j
                                       where j.VeId         = PrimaerdatenAuftrag.VeId
                                         and j.OrderingDate = (Select max(i.OrderingDate)
                                                               from v_OrderingFlatItem i
                                                               where i.VeId         = PrimaerdatenAuftrag.VeId
                                                                 and i.OrderingDate < PrimaerdatenAuftrag.CreatedOn
                                                               group by i.veid)) 
GO
