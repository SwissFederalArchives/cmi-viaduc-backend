namespace CMI.Contract.Messaging
{
    /// <summary>
    ///     Several constants defining the queue names in RabbitMq.
    /// 
    ///     FOR MESSAGES we use this naming schema: m.Subscriber.SubscriberType.MessageTypeName
    ///     1. The letter m for "message"
    ///     2. The name of the class subscribing to the endpoint
    ///     3. The constant name "manager" or "api" as these two are the only ones to be allowed to subscribe to the bus.
    ///     4. The message type name
    /// 
    ///     FOR EVENTS we use this naming schema: e.Subscriber.SubsriberType.MessageTypeName
    ///     1. The letter e for "event"
    ///     2. The name of the class consuming the event. There can/will be more that one subcriber to an event.
    ///     3. The constant name "manager" or "api" as these two are the only ones to be allowed to subscribe to the bus.
    ///     4. The message type name
    /// 
    ///     FOR REQUEST/RESPONSE messags we use this naming schema: r.Initiator.InitiatorType.MessageTypeName
    ///     1. The letter r for "request/response"
    ///     2. The name of the class initiating the request
    ///     3. The constant name "manager" or "api" as these two are the only ones to be allowed to subscribe to the bus.
    ///     4. The message type name
    /// 
    ///     The name of the constant is the abbreviated form of the constant
    /// </summary>
    public static class BusConstants
    {
        // Messages the Harvest Manager subscribes to
        public const string HarvestManagerSyncArchiveRecordMessageQueue = "m.harvest.manager.syncArchiveRecord";

        public const string HarvestManagerResyncArchiveDatabaseMessageQueue = "m.harvest.manager.resyncArchiveDatabase";

        // Events the Harvest Manager subscribes to
        public const string HarvestManagerArchiveRecordUpdatedEventQueue = "e.harvest.manager.archiveRecordUpdated";
        public const string HarvestManagerArchiveRecordRemovedEventQueue = "e.harvest.manager.archiveRecordRemoved";

        // Requests the Management API initiates
        public const string ManagementApiGetHarvestStatusInfoRequestQueue = "r.management.api.getHarvestStatusInfo";
        public const string ManagementApiGetHarvestLogInfoRequestQueue = "r.management.api.getHarvestLogInfo";
        public const string ManagementApiGetDigitizationOrderData = "r.management.api.getDigitizationOrderData";

        // Events the Management Client subscribes to
        public const string ManagementApiAbbyyProgressEventQueue = "e.managementClient.manager.abbyyProgressEvent";
        public const string ManagementApiDocumentConverterServiceStartedQueue = "e.managementClient.manager.documentConverterServiceStartedEvent";

        // Messages the Index Manager subscribes to
        public const string IndexManagerUpdateArchiveRecordMessageQueue = "m.index.manager.updateArchiveRecord";
        public const string IndexManagerAnonymizeArchiveRecordMessageQueue = "m.index.manager.anonymizeArchiveRecord";
        public const string IndexManagerAnonymizeTestMessageQueue = "m.index.manager.anonymizeTest";
        public const string IndexManagerRemoveArchiveRecordMessageQueue = "m.index.manager.removeArchiveRecord";
        public const string IndexManagerFindArchiveRecordMessageQueue = "m.index.manager.findArchiveRecord";
        public const string IndexManagagerRequestBase = "r.index.manager.{0}";
        public const string IndexManagerUpdateIndivTokensMessageQueue = "m.index.manager.updateIndivTokens";
        public const string IndexManagerGetElasticLogRecordsRequestQueue = "m.index.manager.getElasticLogRecords";

        // Mesages the Repository Manager subscribes to
        public const string RepositoryManagerDownloadPackageMessageQueue = "m.repository.manager.downloadPackage";
        public const string RepositoryManagerArchiveRecordAppendPackageMessageQueue = "m.repository.manager.archiveRecordAppendPackage";
        public const string RepositoryManagerReadPackageMetadataMessageQueue = "m.repository.manager.readPackageMetadata";

        // Messages the Asset Manager subscribes to
        public const string AssetManagerExtractFulltextMessageQueue = "m.asset.manager.archiveRecordExtractFulltextFromPackage";
        public const string AssetManagerTransformAssetMessageQueue = "m.asset.manager.transformAsset";
        public const string AssetManagerSchdeduleForPackageSyncMessageQueue = "m.asset.manager.scheduleForPackageSync";
        public const string AssetManagerPrepareForRecognition = "m.asset.manager.prepareForRecognition";
        public const string AssetManagerPrepareForTransformation = "m.asset.manager.prepareForTransformation";
        public const string AssetManagerRecognitionPostProcessing = "m.asset.manager.recognitionPostProcessing";

        public const string AssetManagerUpdatePrimaerdatenAuftragStatusMessageQueue = "m.asset.manager.updatePrimaerdatenAuftragStatus";

        // Events the Asset Manager subscribest to
        public const string AssetManagerAssetReadyEventQueue = "e.asset.manager.assetReady";

        // Requests the Web-API initiates
        public const string WebApiDownloadAssetRequestQueue = "r.web.api.downloadAsset";
        public const string WebApiPrepareAssetRequestQueue = "r.web.api.prepareAsset";
        public const string WebApiGetAssetStatusRequestQueue = "r.web.api.getAssetStatus";

        // Events the Repository Manager Test clients subscribes to
        public const string RepositoryTestPackageDownloadedEventQueue = "e.repository.test.packageDownloaded";

        public const string NotificationManagerMessageQueue = "m.notification.manager.email";

        public const string CacheDoesExistRequestQueue = "r.cache.manager.doesExist";

        public const string CacheConnectionInfoRequestQueue = "r.cache.manager.connectionInfo";
        public const string CacheDeleteFile = "m.cache.manager.deleteFile";

        // Events the Parameter Service subscribes to
        public const string ParameterServiceRequestQueue = "r.parameter.manager.getParameter";

        // Requests the document converter handles
        public const string DocumentConverterJobInitRequestQueue = "r.documentConverter.manager.JobInitRequest";
        public const string DocumentConverterJobEndRequestQueue = "r.documentConverter.manager.JobEndRequest";
        public const string DocumentConverterConversionStartRequestQueue = "r.documentConverter.manager.conversionStartRequest";
        public const string DocumentConverterExtractionStartRequestQueue = "r.documentConverter.manager.extractionStartRequest";
        public const string DocumentConverterSupportedFileTypesRequestQueue = "r.documentConverter.manager.supportedFileTypesRequest";

        // Events the Viaduc Service subscribes to
        public const string ReadUserInformationQueue = "r.viaduc.manager.readuserinformation";
        public const string ReadStammdatenQueue = "r.viaduc.manager.readstammdaten";
        public const string ViaducManagerRequestBase = "r.viaduc.manager.{0}";


        // Request/Response base URL for OrderManager
        public const string OrderManagagerRequestBase = "r.order.manager.{0}";
        
        public const string OrderManagerGetStatusHistoryForOrderItemRequestQueue = "r.order.manager.getstatushistoryfororderitemrequest";

        public const string OrderManagerFindOrderingHistoryForVeRequestQueue = "r.order.manager.findorderhistoryforverequest";

        public const string OrderManagerFindOrderItemsRequestQueue = "r.order.manager.findOrderItems";
        public const string OrderManagerGetOrderingRequestQueue = "r.order.manager.getOrdering";

        public const string OrderManagerAssetReadyEventQueue = "e.order.manager.assetReady";

        public const string OrderManagerSetStatusAushebungBereitRequestQueue = "r.order.manager.setstatusaushebungbereit";
        public const string OrderManagerSetStatusDigitalisierungExternRequestQueue = "r.order.manager.setstatusdigitalisierungextern";

        public const string OrderManagerSetStatusDigitalisierungAbgebrochenRequestQueue = "r.order.manager.setstatusdigitalisierungabgebrochen";
        public const string OrderManagerUpdateOrderDetailRequestQueue = "r.order.manager.updateorderdetail";

        public const string OrderManagerSetStatusZumReponierenBereitRequestQueue = "r.order.manager.zumreponierenbereit";
        public const string OrderManagerMarkOrderAsFaultedQueue = "r.order.manager.markOrderAsFaulted";
        public const string OrderManagerResetFaultedOrdersQueue = "r.order.manager.resetFaultedOrders";
        public const string OrderManagerMahnungVersendenRequestQueue = "r.order.manager.mahnungVersenden";
        public const string OrderManagerErinnerungVersendenRequestQueue = "r.order.manager.erinnerungVersenden";
        public const string EntscheidFreigabeHinterlegenRequestQueue = "r.order.manager.entscheidfreigabehinterlegen";
        public const string RecalcIndivTokens = "r.order.manager.RecalcIndivTokens";
        public const string DigitalisierungAusloesenRequestQueue = "r.order.manager.digitalisierungAusloesen";
        public const string DigitalisierungsAuftragErledigtEvent = "e.order.manager.digitalisierungsAuftragErledigt";
        public const string DigitalisierungsAuftragErledigtEventError = "e.order.manager.digitalisierungsAuftragErledigt_error";
        public const string BenutzungskopieAuftragErledigtEvent = "e.order.manager.benutzungskopieAuftragErledigt";
        public const string BenutzungskopieAuftragErledigtEventError = "e.order.manager.benutzungskopieAuftragErledigt_error";

        public const string EntscheidGesuchHinterlegenRequestQueue = "r.order.manager.entscheidgesuchhinterlegen";
        public const string InVorlageExportierenRequestQueue = "r.order.manager.invorlageexportieren";
        public const string ReponierenRequestQueue = "r.order.manager.reponieren";
        public const string AbschliessenRequestQueue = "r.order.manager.abschliessen";
        public const string AbbrechenRequestQueue = "r.order.manager.abbrechen";
        public const string ZuruecksetzenRequestQueue = "r.order.manager.zuruecksetzen";
        public const string AuftraegeAusleihenRequestQueue = "r.order.manager.auftraegeausleihen";
        public const string AushebungsauftraegeDruckenRequestQueue = "r.order.manager.aushebungsauftraegedrucken";

        public const string MonitoringServiceHeartbeatRequestQueue = "r.monitoring.heartbeat.{0}";
        public const string MonitoringElasticSearchTestQueue = "r.monitoring.elasticsearchRequest";
        public const string MonitoringAisDbCheckQueue = "r.monitoring.aisDbCheck";
        public const string MonitoringAbbyyOcrTestQueue = "r.monitoring.abbyyOcrTest";
        public const string MonitoringDirCheckQueue = "r.monitoring.checkDir";
        public const string MonitoringDocumentConverterInfoQueue = "r.monitoring.documentConverterInfo";

        // Events the Order Manager subscribes to
        public const string OrderManagerArchiveRecordUpdatedEventQueue = "e.order.manager.archiveRecordUpdated";

        // Messages the Onboarding Manager subscribes to
        public const string OnboardingManagerStartProcessMessageQueue = "m.onboarding.manager.startProcess";
        public const string OnboardingManagerHandleCallbackMessageQueue = "m.onboarding.manager.handleCallback";
    }
}