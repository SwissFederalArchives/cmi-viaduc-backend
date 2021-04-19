using System.Collections.Generic;
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
        ///     Updates an archive record in ElasticSearch
        /// </summary>
        /// <param name="updateContext">The update context with the information from the bus.</param>
        void UpdateArchiveRecord(ConsumeContext<IUpdateArchiveRecord> updateContext);

        /// <summary>
        ///     Removes an archive record from ElasticSearch
        /// </summary>
        /// <param name="removeContext">The remove context.</param>
        void RemoveArchiveRecord(ConsumeContext<IRemoveArchiveRecord> removeContext);

        ElasticArchiveRecord FindArchiveRecord(string archiveRecordId, bool includeFulltextContent);

        /// <summary>
        ///     Gets all the archive records for a specific primary data package.
        ///     Most often this will return just the ordered dossier, e.g. 1 record.
        ///     If that dossier has children, it will also return all the children
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>List&lt;ElasticArchiveRecord&gt;.</returns>
        List<ElasticArchiveRecord> GetArchiveRecordsForPackage(string packageId);

        void UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens);
    }
}