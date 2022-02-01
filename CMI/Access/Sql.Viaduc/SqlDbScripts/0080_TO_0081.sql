CREATE OR ALTER View v_Primaerdatenaufbereitung
AS
SELECT    
	ofi.OrderingDate, 
	ofi.OrderingType, 
	ofi.ItemId as OrderItemId, 
	ofi.Dossiertitel,
	PrimaerdatenAuftrag.VeId,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 1) AS NeuEingegangen,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 4) AS FreigabePruefen,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 6) AS FuerDigitalisierungBereit,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 7) AS FuerAushebungBereit,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 9) AS Ausgeliegen,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 12) AS ZumReponierenBereit,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 13) AS Abgeschlossen,
	(Select max(StatusChangeDate) from StatusHistory where OrderItemId = ofi.ItemId and ToStatus = 5) AS Abgebrochen,
	PrimaerdatenAuftrag.PrimaerdatenAuftragId,
	PrimaerdatenAuftrag.AufbereitungsArt, 
	PrimaerdatenAuftrag.GroesseInBytes,
	PrimaerdatenAuftrag.PackageMetadata,
	-- {"MutationId":13136930,"ArchiveRecord":{"ElasticPr
	CASE PrimaerdatenAuftrag.AufbereitungsArt 
		WHEN 'Sync' THEN CAST( Substring(PrimaerdatenAuftrag.Workload, Len('{"MutationId":')+1, Charindex(',"Archiv', PrimaerdatenAuftrag.Workload, 1)- Len('{"MutationId":')-1) as int)
		ELSE null
	END as MutationsId,
	CASE isnull(ofi.ItemId, -1) 
		WHEN -1 THEN 'Digitale Ablieferung'
		ELSE 'Vecteur'
	END as Quelle,
	PrimaerdatenAuftrag.GeschaetzteAufbereitungszeit,
	(Select max(CreatedOn) from PrimaerdatenAuftragLog where PrimaerdatenAuftragId = PrimaerdatenAuftrag.PrimaerdatenAuftragId and Status = 'Registriert') AS Registriert,
	(Select max(CreatedOn) from PrimaerdatenAuftragLog where PrimaerdatenAuftragId = PrimaerdatenAuftrag.PrimaerdatenAuftragId and Status = 'AuftragGestartet') AS LetzterAufbereitungsversuch,
	(Select min(CreatedOn) from PrimaerdatenAuftragLog where PrimaerdatenAuftragId = PrimaerdatenAuftrag.PrimaerdatenAuftragId and Status = 'AuftragGestartet') AS ErsterAufbereitungsversuch,
	(Select max(CreatedOn) from PrimaerdatenAuftragLog where PrimaerdatenAuftragId = PrimaerdatenAuftrag.PrimaerdatenAuftragId and Status = 'AuftragErledigt') AS AuftragErledigt,
	(Select max(CreatedOn) from PrimaerdatenAuftragLog where PrimaerdatenAuftragId = PrimaerdatenAuftrag.PrimaerdatenAuftragId and Status = 'ImCacheAbgelegt') AS ImCacheAbgelegt,
	(Select count(pa.VeId) from PrimaerdatenAuftrag pa where pa.VeId = PrimaerdatenAuftrag.VeId and pa.AufbereitungsArt = 'Download' and pa.CreatedOn > ofi.OrderingDate) AS AnzahlVersucheDownload,
	(Select count(pa.VeId) from PrimaerdatenAuftrag pa where pa.VeId = PrimaerdatenAuftrag.VeId and pa.AufbereitungsArt = 'Sync' and pa.CreatedOn > ofi.OrderingDate) AS AnzahlVersucheSync
FROM      
	PrimaerdatenAuftrag 
	LEFT JOIN v_OrderingFlatItem ofi
		ON PrimaerdatenAuftrag.VeId = ofi.VeId
		AND PrimaerdatenAuftrag.CreatedOn > ofi.OrderingDate 
	

	
GO


