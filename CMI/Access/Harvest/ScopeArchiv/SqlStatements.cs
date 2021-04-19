    namespace CMI.Access.Harvest.ScopeArchiv
    {
        internal class SqlStatements
        {
            public const string SqlDataElementsSelect = "";
    
            public const string SqlArchiveRecordSelect = "";
    
            public const string SqlNodeContext = "";
    
            public const string SqlArchiveRecordContainers = "";
    
            public const string SqlArchiveRecordDescriptors = "";
            
            // We order the mutation records according to the archive plan.
            // Thus parenting nodes are inserted before their children
            // and population starts in a "top down" manner
            public const string SqlMutationsRecords = "";
    
            public const string SqlArchiveRecordReferences = "";
    
            public const string SqlArchiveRecordNodeInfo= "";
    
            public const string SqlArchivePlanInfo = "";
    
            public const string SqlUpdateMutationActionLog = "";
    
            public const string ResetFailedOrLostOperations = "";
            public const string GetArchiveRecordSecurityInfo = "";
            public const string GetArchiveRecordPrimaryDataSecurityInfo = "";
    
            public const string InitiateFullResync = "";
    
            public const string HarvestStatusInfo = "";
    
            public const string HarvestLogInfo = "";
    
            public const string HarvestLogInfoDetail = "";
            public const string FondsOverviewList = "";
    
            public const string GetAccession = "";
            public const string GetDetailDataForDataElement = "";
    
            public const string SqlArchiveRecordForContainer = "";
            public const string OrderDetailDataSelect = "";
            public const string OrderDetailDataSelectForContainer = "";
            public const string OrderDetailDataSelectForChildRecords = "";
    
        }
    }
