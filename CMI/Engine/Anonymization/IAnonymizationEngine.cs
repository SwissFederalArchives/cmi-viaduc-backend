using System.Threading.Tasks;
using CMI.Contract.Common;

namespace CMI.Engine.Anonymization
{
    public interface IAnonymizationEngine {

        /// <summary>
        ///  Uses an external anonymization service to anonymize parts of the elasticArchiveRecord
        /// </summary>
        /// <param name="elasticArchiveRecord"></param>
        /// <returns></returns>
        Task<ElasticArchiveDbRecord> AnonymizeArchiveRecordAsync(ElasticArchiveDbRecord elasticArchiveRecord);
    }
}
