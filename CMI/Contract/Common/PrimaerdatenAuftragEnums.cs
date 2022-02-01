namespace CMI.Contract.Common
{
    public enum AufbereitungsArtEnum
    {
        Sync,
        Download
    }

    public enum AufbereitungsStatusEnum
    {
        Registriert,
        AuftragGestartet,
        PrimaerdatenExtrahiert,
        ZipDateiErzeugt,
        PaketTransferiert,
        ZipEntpackt,
        PreprocessingAbgeschlossen,
        OCRAbgeschlossen,
        AssetUmwandlungAbgeschlossen,
        IndizierungAbgeschlossen,
        ImCacheAbgelegt,
        AuftragErledigt
    }

    public enum AufbereitungsServices
    {
        AssetService,
        CacheService,
        DocumentConverterService,
        HarvestService,
        IndexService,
        RepositoryService
    }
}