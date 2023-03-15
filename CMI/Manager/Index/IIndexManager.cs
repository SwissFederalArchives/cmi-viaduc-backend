using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;

namespace CMI.Manager.Index
{
    /// <summary>
    ///     The IIndexManager interface provides methods for processing <see cref="ArchiveRecord" /> in ElasticSearch
    /// </summary>
    public interface IIndexManager
    {
        /// <summary>
        /// Updates an archive record in ElasticSearch
        /// </summary>
        /// <param name="elasticArchiveRecord"></param>
        /// <returns></returns>
        ElasticArchiveRecord UpdateArchiveRecord(ElasticArchiveRecord elasticArchiveRecord);

        /// <summary>
        ///     Removes an archive record from ElasticSearch
        /// </summary>
        /// <param name="removeContext">The remove context.</param>
        void RemoveArchiveRecord(ConsumeContext<IRemoveArchiveRecord> removeContext);

        ElasticArchiveRecord FindArchiveRecord(string archiveRecordId, bool includeFulltextContent, bool useUnanonymizedData);

        /// <summary>
        ///     Gets all the archive records for a specific primary data package.
        ///     Most often this will return just the ordered dossier, e.g. 1 record.
        ///     If that dossier has children, it will also return all the children
        /// </summary>
        /// <param name="archiveRecordId">The id of the archive record that links to the package</param>
        /// <returns>List&lt;ElasticArchiveRecord&gt;.</returns>
        List<ElasticArchiveRecord> GetArchiveRecordsForPackage(string archiveRecordId);

        void UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens, string[] fieldAccessTokens);

        /// <summary>
        /// Sends the Record to Anonymize Engine 
        /// </summary>
        /// <param name="elasticArchiveRecord"></param>
        /// <returns></returns>
        Task<ElasticArchiveDbRecord> AnonymizeArchiveRecordAsync(ElasticArchiveDbRecord elasticArchiveRecord);

        /// <summary>
        /// Convents an ArchiveRecord to an ElasticArchiveRecord
        /// </summary>
        /// <param name="archiveRecord"></param>
        /// <returns></returns>
        ElasticArchiveRecord ConvertArchiveRecord(ArchiveRecord archiveRecord);

        /// <summary>
        /// Check if this record has a ManuelleKorrektur and apply those if required.
        /// Also set anonymized texts for Archivplans, ParentArchivplans and References 
        /// </summary>
        /// <param name="elasticArchiveRecord"></param>
        /// <returns>The eventually updated record</returns>
        ElasticArchiveDbRecord SyncAnonymizedTextsWithRelatedRecords(ElasticArchiveDbRecord elasticArchiveRecord);

        void DeletePossiblyExistingManuelleKorrektur(ElasticArchiveRecord elasticArchiveRecord);

        /// <summary>
        /// Updates the children and references of the passed record, so that the title and other
        /// information are in sync. 
        /// </summary>
        /// <param name="archiveRecordId"></param>
        void UpdateDependentRecords(string archiveRecordId);

        /// <summary>
        /// Checks the references of an unprotected record, if those point
        /// to a protected record. If so, we update the references only.
        /// In theory the UpdateDependentRecords of the reference should do the trick,
        /// but only if that record already contains new reference.
        /// If an unprotected record is synced for the first time and has a link to a
        /// protected record, the protected record does not yet contain the link to that new record.
        /// Only after syncing the protected record again, the state would be ok.
        /// But as we can't be sure that this will happen, we update the references
        /// </summary>
        /// <param name="archiveRecordId"></param>
        void UpdateReferencesOfUnprotectedRecord(string archiveRecordId);
    }
}
