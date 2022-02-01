using System.Collections.Generic;
using CMI.Access.Harvest.ScopeArchiv.DataSets;
using CMI.Contract.Common;
using CMI.Contract.Harvest;

namespace CMI.Access.Harvest.ScopeArchiv
{
    public interface IAISDataProvider
    {
        ArchiveRecordDataSet.ArchiveRecordRow GetArchiveRecordRow(long recordId);
        List<MutationRecord> GetPendingMutations();
        List<SyncInfoForReport> GetSyncInfoForReport(List<int> mutationsIds);
        ArchivePlanInfoDataSet LoadArchivePlanInfo(long[] recordIdList);
        ContainerDataSet LoadContainers(long recordId);
        DescriptorDataSet LoadDescriptors(long recordId);
        DetailDataDataSet LoadDetailData(long recordId);
        NodeContext LoadNodeContext(long recordId);
        NodeInfoDataSet LoadNodeInfo(long recordId);
        ReferencesDataSet LoadReferences(long recordId);
        int UpdateMutationStatus(MutationStatusInfo info);
        int BulkUpdateMutationStatus(List<MutationStatusInfo> infos);
        int ResetFailedSyncOperations(int maxRetries);
        List<string> LoadMetadataSecurityTokens(long recordId);
        PrimaryDataSecurityTokenResult LoadPrimaryDataSecurityTokens(long recordId);
        int InitiateFullResync();
        HarvestStatusInfo GetHarvestStatusInfo(QueryDateRange dataRange);
        HarvestLogInfoResult GetHarvestLogInfo(HarvestLogInfoRequest request);
        List<FondLink> LoadFondLinks();

        AccessionDataSet.AcessionRecordRow GetLinkedAccessionToArchiveRecord(long recordId);

        DetailDataDataSet.DetailDataDataTable GetDetailDataForElement(long recordId, int dataElementId);
        string GetBusinessObjectIdName(long recordId);
        List<OrderDetailData> GetChildrenRecordOrderDetailDataForArchiveRecord(long recordId);
        List<OrderDetailData> GetArchiveRecordOrderDetailDataForContainer(long containerId);
        string GetDbVersion();
        OrderDetailData LoadOrderDetailData(long recordId);
    }
}