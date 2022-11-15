using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Access.Common
{
    public interface ISearchIndexDataAccess
    {
        void UpdateDocument(ElasticArchiveRecord elasticArchiveRecord);
        void RemoveDocument(string archiveRecordId);

        ElasticArchiveRecord FindDocument(string archiveRecordId, bool includeFulltextContent);

        /// <summary>
        /// Returns an archive record without anonymization
        /// </summary>
        /// <param name="archiveRecordId"></param>
        /// <param name="includeFulltextContent"></param>
        /// <returns></returns>
        ElasticArchiveRecord FindDocumentWithoutSecurity(string archiveRecordId, bool includeFulltextContent);

        ElasticArchiveDbRecord FindDbDocument(string archiveRecordIdOrSignature, bool includeFulltextContent);

        /// <summary>
        ///     Finds the document by its package identifier.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>ElasticArchiveRecord.</returns>
        ElasticArchiveRecord FindDocumentByPackageId(string packageId);

        /// <summary>
        ///     Gets the children to an archive record.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <param name="allLevels">
        ///     if set to <c>true</c> all children are returned. If set to <c>false</c> only the direct
        ///     children are returned
        /// </param>
        /// <returns>IEnumerable&lt;ElasticArchiveRecord&gt;.</returns>
        IEnumerable<ElasticArchiveRecord> GetChildren(string archiveRecordId, bool allLevels);

        /// <summary>
        ///     Gets the children to an archive record using the unprotected version of the data
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <param name="allLevels">
        ///     if set to <c>true</c> all children are returned. If set to <c>false</c> only the direct
        ///     children are returned
        /// </param>
        /// <returns>IEnumerable&lt;ElasticArchiveRecord&gt;.</returns>
        IEnumerable<ElasticArchiveRecord> GetChildrenWithoutSecurity(string archiveRecordId, bool allLevels);

        void UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens, string[] fieldAccessTokens);
    }
}