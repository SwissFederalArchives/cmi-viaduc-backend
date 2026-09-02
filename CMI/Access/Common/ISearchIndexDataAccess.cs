using CMI.Contract.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMI.Access.Common
{
    public interface ISearchIndexDataAccess
    {
        Task UpdateDocument(ElasticArchiveRecord elasticArchiveRecord);
        Task RemoveDocument(string archiveRecordId);

        Task<ElasticArchiveRecord> FindDocument(string archiveRecordId, MetadataToExclude metadataToExclude);

        /// <summary>
        /// Returns an archive record without anonymization
        /// </summary>
        /// <param name="archiveRecordId"></param>
        /// <param name="metadataToExclude"></param>
        /// <returns></returns>
        Task<ElasticArchiveRecord> FindDocumentWithoutSecurity(string archiveRecordId, MetadataToExclude metadataToExclude);

        Task<ElasticArchiveDbRecord> FindDbDocument(string archiveRecordIdOrSignature, MetadataToExclude metadataToExclude);

        /// <summary>
        ///     Finds the document by its package identifier.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>ElasticArchiveRecord.</returns>
        Task<ElasticArchiveRecord> FindDocumentByPackageId(string packageId);

        /// <summary>
        /// Finds a document by a set of query terms.
        /// The key is the field name and the value is the value to search for.
        /// The given terms are combined with AND.
        /// </summary>
        /// <param name="queryTerms"></param>
        /// <param name="pageSize"></param>
        Task<List<ElasticArchiveRecord>> FindDocument(Dictionary<string, string> queryTerms, int pageSize);

        /// <summary>
        ///     Gets the children to an archive record.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <param name="allLevels">
        ///     if set to <c>true</c> all children are returned. If set to <c>false</c> only the direct
        ///     children are returned
        /// </param>
        /// <returns>IEnumerable&lt;ElasticArchiveRecord&gt;.</returns>
       Task<IEnumerable<ElasticArchiveRecord>> GetChildren(string archiveRecordId, string externalKeyId, bool allLevels);

        /// <summary>
        ///     Gets the children to an archive record using the unprotected version of the data
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <param name="allLevels">
        ///     if set to <c>true</c> all children are returned. If set to <c>false</c> only the direct
        ///     children are returned
        /// </param>
        /// <returns>IEnumerable&lt;ElasticArchiveRecord&gt;.</returns>
       Task<IEnumerable<ElasticArchiveRecord>> GetChildrenWithoutSecurity(string archiveRecordId, string externalKeyId, bool allLevels);

        Task UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens, string[] fieldAccessTokens);
    }
}